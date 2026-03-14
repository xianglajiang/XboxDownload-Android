using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;

namespace XboxDownload.Android.Services;

[Service(Name = "com.xboxdownload.app.XboxDownloadVpnService", 
         Label = "XboxDownload VPN", 
         Permission = "android.permission.BIND_VPN_SERVICE")]
[IntentFilter(new[] { "android.net.VpnService" })]
public class XboxDownloadVpnService : VpnService
{
    public const string ACTION_CONNECT = "com.xboxdownload.CONNECT";
    public const string ACTION_DISCONNECT = "com.xboxdownload.DISCONNECT";

    private const int DNS_PORT = 53;
    private const string VPN_ADDRESS = "10.0.0.2";
    private const string VPN_ROUTE = "0.0.0.0";
    
    private ParcelFileDescriptor _vpnInterface;
    private CancellationTokenSource _cts;
    private Thread _dnsThread;

    public static Dictionary<string, List<string>> Ipv4ServiceMap = new Dictionary<string, List<string>>();
    public static Dictionary<string, List<string>> Ipv6ServiceMap = new Dictionary<string, List<string>>();
    public static Dictionary<string, List<string>> Ipv4HostMap = new Dictionary<string, List<string>>();
    public static Dictionary<string, List<string>> Ipv6HostMap = new Dictionary<string, List<string>>();

    public static string UpstreamDns { get; set; } = "114.114.114.114";
    
    public static bool IsRunning { get; private set; }
    public static event EventHandler<bool> StateChanged;

    public override void OnCreate()
    {
        base.OnCreate();
        CreateNotificationChannel();
    }

    public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
    {
        if (intent == null)
        {
            return StartCommandResult.NotSticky;
        }

        switch (intent.Action)
        {
            case ACTION_CONNECT:
                StartVpn();
                return StartCommandResult.Sticky;
            
            case ACTION_DISCONNECT:
                StopVpn();
                return StartCommandResult.NotSticky;
            
            default:
                return IsRunning ? StartCommandResult.Sticky : StartCommandResult.NotSticky;
        }
    }

    private void StartVpn()
    {
        if (IsRunning) return;

        try
        {
            var builder = new Builder(this)
                .SetSession("XboxDownload")
                .SetMtu(1500)
                .AddAddress(VPN_ADDRESS, 32)
                .AddDnsServer(UpstreamDns)
                .AddRoute(VPN_ROUTE, 0);

            _vpnInterface = builder.Establish();
            
            _cts = new CancellationTokenSource();
            _dnsThread = new Thread(() => ListenDns(_cts.Token))
            {
                IsBackground = true,
                Name = "XboxDownload DNS"
            };
            _dnsThread.Start();

            IsRunning = true;
            StateChanged?.Invoke(this, true);
            
            StartForeground(1, CreateNotification());

            Android.Util.Log.Info("XboxDownload", "VPN Service started");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("XboxDownload", "Failed to start VPN: " + ex.Message);
            StopVpn();
        }
    }

    private void StopVpn()
    {
        _cts?.Cancel();
        
        _dnsThread?.Join(3000);
        _dnsThread = null;

        _vpnInterface?.Close();
        _vpnInterface = null;

        IsRunning = false;
        StateChanged?.Invoke(this, false);
        
        StopForeground(StopForegroundFlags.Remove);
        StopSelf();

        Android.Util.Log.Info("XboxDownload", "VPN Service stopped");
    }

    private void ListenDns(CancellationToken token)
    {
        Java.Net.DatagramSocket dnsSocket = null;
        try
        {
            dnsSocket = new Java.Net.DatagramSocket(DNS_PORT);
            dnsSocket.SoTimeout = 5000;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var buffer = new byte[512];
                    var packet = new Java.Net.DatagramPacket(buffer, buffer.Length);
                    
                    try
                    {
                        dnsSocket.Receive(packet);
                    }
                    catch (Java.Net.SocketTimeoutException)
                    {
                        continue;
                    }

                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            ProcessDnsQuery(buffer, packet.Address, packet.Port, dnsSocket);
                        }
                        catch (Exception ex)
                        {
                            Android.Util.Log.Warn("XboxDownload", "DNS error: " + ex.Message);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Android.Util.Log.Warn("XboxDownload", "Listen error: " + ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("XboxDownload", "DNS socket error: " + ex.Message);
        }
        finally
        {
            dnsSocket?.Close();
        }
    }

    private void ProcessDnsQuery(byte[] buffer, Java.Net.InetAddress clientIp, int clientPort, Java.Net.DatagramSocket socket)
    {
        if (buffer.Length < 12) return;

        var transactionId = (ushort)((buffer[0] << 8) | buffer[1]);
        var flags = (ushort)((buffer[2] << 8) | buffer[3]);
        
        if ((flags & 0x8000) != 0) return;
        
        var questionCount = (ushort)((buffer[4] << 8) | buffer[5]);
        if (questionCount == 0) return;

        var domain = ParseDomainName(buffer, 12);
        if (string.IsNullOrEmpty(domain)) return;

        var domainLower = domain.ToLower();

        List<string> ips = null;
        
        if (Ipv4ServiceMap.TryGetValue(domainLower, out ips) || Ipv6ServiceMap.TryGetValue(domainLower, out ips))
        {
        }
        else
        {
            foreach (var kvp in Ipv4HostMap)
            {
                if (domainLower.EndsWith(kvp.Key) || domainLower == kvp.Key)
                {
                    ips = kvp.Value;
                    break;
                }
            }
        }

        if (ips != null && ips.Count > 0)
        {
            var response = BuildDnsResponse(buffer, transactionId, domain, ips);
            var responsePacket = new Java.Net.DatagramPacket(response, response.Length, clientIp, clientPort);
            socket.Send(responsePacket);
            
            Android.Util.Log.Debug("XboxDownload", "Intercepted: " + domain + " -> " + ips[0]);
        }
    }

    private string ParseDomainName(byte[] buffer, int offset)
    {
        var parts = new List<string>();
        var pos = offset;

        while (pos < buffer.Length)
        {
            var length = buffer[pos++];
            if (length == 0) break;
            
            if ((length & 0xC0) == 0xC0) break;
            
            if (pos + length > buffer.Length) break;
            
            var part = System.Text.Encoding.ASCII.GetString(buffer, pos, length);
            parts.Add(part);
            pos += length;
        }

        return parts.Count > 0 ? string.Join(".", parts) : string.Empty;
    }

    private byte[] BuildDnsResponse(byte[] query, ushort transactionId, string domain, List<string> ips)
    {
        var response = new List<byte>();
        
        response.Add((byte)(transactionId >> 8));
        response.Add((byte)(transactionId & 0xFF));
        response.Add(0x81);
        response.Add(0x80);
        response.Add(0x00);
        response.Add(0x01);
        response.Add((byte)(ips.Count >> 8));
        response.Add((byte)(ips.Count & 0xFF));
        response.Add(0x00);
        response.Add(0x00);
        response.Add(0x00);
        response.Add(0x00);

        foreach (var part in domain.Split('.'))
        {
            response.Add((byte)part.Length);
            response.AddRange(System.Text.Encoding.ASCII.GetBytes(part));
        }
        response.Add(0x00);
        response.Add(0x00);
        response.Add(0x01);
        response.Add(0x00);
        response.Add(0x01);

        var ttl = 100;
        foreach (var ip in ips)
        {
            response.Add(0xC0);
            response.Add(0x0C);
            response.Add(0x00);
            response.Add(0x01);
            response.Add(0x00);
            response.Add(0x01);
            response.Add((byte)(ttl >> 24));
            response.Add((byte)((ttl >> 16) & 0xFF));
            response.Add((byte)((ttl >> 8) & 0xFF));
            response.Add((byte)(ttl & 0xFF));
            
            var ipParts = ip.Split('.');
            if (ipParts.Length == 4)
            {
                response.Add(0x00);
                response.Add(0x04);
                foreach (var p in ipParts)
                {
                    response.Add(byte.Parse(p));
                }
            }
        }

        return response.ToArray();
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(
                "xboxdownload_vpn",
                "VPN Service",
                NotificationImportance.Low)
            {
                Description = "XboxDownload VPN Service"
            };
            
            var manager = GetSystemService(NotificationService) as NotificationManager;
            manager?.CreateNotificationChannel(channel);
        }
    }

    private Notification CreateNotification()
    {
        var pendingIntent = PendingIntent.GetActivity(
            this, 0,
            new Intent(this, typeof(MainActivity)),
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        return new Notification.Builder(this, "xboxdownload_vpn")
            .SetContentTitle("XboxDownload")
            .SetContentText("VPN Service Running")
            .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
            .SetContentIntent(pendingIntent)
            .SetOngoing(true)
            .Build();
    }

    public override void OnDestroy()
    {
        StopVpn();
        base.OnDestroy();
    }

    public override void OnRevoke()
    {
        StopVpn();
        base.OnRevoke();
    }
}
