using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using XboxDownload.Android.Services;

namespace XboxDownload.Android.Fragments;

public class HomeFragment : Androidx.Fragment.App.Fragment
{
    private Switch switchVpn;
    private TextView txtStatus;
    private TextView txtLocalIp;
    private TextView txtDnsServer;
    private Button btnRefresh;

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        return inflater.Inflate(Resource.Layout.FragmentHome, container, false);
    }

    public override void OnViewCreated(View view, Bundle savedInstanceState)
    {
        base.OnViewCreated(view, savedInstanceState);

        switchVpn = view.FindViewById<Switch>(Resource.Id.switchVpn);
        txtStatus = view.FindViewById<TextView>(Resource.Id.txtStatus);
        txtLocalIp = view.FindViewById<TextView>(Resource.Id.txtLocalIp);
        txtDnsServer = view.FindViewById<TextView>(Resource.Id.txtDnsServer);
        btnRefresh = view.FindViewById<Button>(Resource.Id.btnRefresh);

        XboxDownloadVpnService.StateChanged += OnVpnStateChanged;

        UpdateVpnStatus();

        switchVpn.CheckedChange += OnVpnSwitchChanged;
        btnRefresh.Click += (s, e) => UpdateVpnStatus();
    }

    private void OnVpnSwitchChanged(object sender, CompoundButton.CheckedChangeEventArgs e)
    {
        if (e.IsChecked)
        {
            StartVpn();
        }
        else
        {
            StopVpn();
        }
    }

    private void StartVpn()
    {
        var intent = new Intent(Activity, typeof(XboxDownloadVpnService));
        intent.SetAction(XboxDownloadVpnService.ACTION_CONNECT);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            Activity.StartForegroundService(intent);
        }
        else
        {
            Activity.StartService(intent);
        }
    }

    private void StopVpn()
    {
        var intent = new Intent(Activity, typeof(XboxDownloadVpnService));
        intent.SetAction(XboxDownloadVpnService.ACTION_DISCONNECT);
        Activity.StartService(intent);
    }

    private void OnVpnStateChanged(object sender, bool isRunning)
    {
        Activity?.RunOnUiThread(() =>
        {
            UpdateVpnStatus();
        });
    }

    private void UpdateVpnStatus()
    {
        if (switchVpn == null || txtStatus == null) return;

        var isRunning = XboxDownloadVpnService.IsRunning;
        switchVpn.Checked = isRunning;
        txtStatus.Text = isRunning ? "VPN 运行中" : "VPN 已停止";
        txtStatus.SetTextColor(isRunning ? Android.Graphics.Color.Green : Android.Graphics.Color.Red);
        txtLocalIp.Text = "本机IP: " + GetLocalIpAddress();
        txtDnsServer.Text = "DNS服务器: " + XboxDownloadVpnService.UpstreamDns;
    }

    private string GetLocalIpAddress()
    {
        try
        {
            var interfaces = Java.Net.NetworkInterface.NetworkInterfaces;
            while (interfaces.HasMoreElements)
            {
                var networkInterface = interfaces.NextElement();
                var addresses = networkInterface.InetAddresses;
                while (addresses.HasMoreElements)
                {
                    var address = addresses.NextElement();
                    if (!address.IsLoopbackAddress && address is Java.Net.Inet4Address)
                    {
                        return address.HostAddress;
                    }
                }
            }
        }
        catch { }
        return "Unknown";
    }

    public override void OnDestroyView()
    {
        XboxDownloadVpnService.StateChanged -= OnVpnStateChanged;
        base.OnDestroyView();
    }
}
