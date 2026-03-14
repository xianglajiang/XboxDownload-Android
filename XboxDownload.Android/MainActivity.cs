using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using XboxDownload.Android.Fragments;

namespace XboxDownload.Android;

[Activity(Label = "XboxDownload", MainLauncher = true)]
public class MainActivity : Activity
{
    private const int VPN_REQUEST_CODE = 100;

    private TextView txtStatus;
    private Button btnStartVpn;
    private RadioGroup bottomNav;

    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        SetContentView(Resource.Layout.MainActivity);
        
        txtStatus = FindViewById<TextView>(Resource.Id.txtStatus);
        btnStartVpn = FindViewById<Button>(Resource.Id.btnStartVpn);
        bottomNav = FindViewById<RadioGroup>(Resource.Id.bottom_navigation);
        
        SetupNavigation();
        
        XboxDownload.Android.Services.XboxDownloadVpnService.StateChanged += OnVpnStateChanged;
        
        UpdateVpnStatus();
        
        btnStartVpn.Click += OnVpnButtonClick;
    }

    private void SetupNavigation()
    {
        if (SupportFragmentManager.FindFragmentById(Resource.Id.fragment_container) == null)
        {
            SupportFragmentManager.BeginTransaction()
                .Add(Resource.Id.fragment_container, new HomeFragment())
                .Commit();
        }
        
        bottomNav.CheckedChange += (s, e) =>
        {
            Androidx.Fragment.App.Fragment fragment = e.CheckedId switch
            {
                Resource.Id.nav_home => new HomeFragment(),
                Resource.Id.nav_speedtest => new SpeedTestFragment(),
                Resource.Id.nav_hosts => new HostFragment(),
                Resource.Id.nav_store => new StoreFragment(),
                Resource.Id.nav_settings => new SettingsFragment(),
                _ => new HomeFragment()
            };
            
            SupportFragmentManager.BeginTransaction()
                .Replace(Resource.Id.fragment_container, fragment)
                .Commit();
        };
    }

    private void OnVpnButtonClick(object sender, EventArgs e)
    {
        var vpnService = typeof(XboxDownload.Android.Services.XboxDownloadVpnService);
        
        if (XboxDownload.Android.Services.XboxDownloadVpnService.IsRunning)
        {
            var intent = new Intent(this, vpnService);
            intent.SetAction(XboxDownload.Android.Services.XboxDownloadVpnService.ACTION_DISCONNECT);
            StartService(intent);
        }
        else
        {
            var intent = Android.Net.VpnService.Prepare(this);
            if (intent != null)
            {
                StartActivityForResult(intent, VPN_REQUEST_CODE);
            }
            else
            {
                StartVpn();
            }
        }
    }

    private void StartVpn()
    {
        var vpnService = typeof(XboxDownload.Android.Services.XboxDownloadVpnService);
        var intent = new Intent(this, vpnService);
        intent.SetAction(XboxDownload.Android.Services.XboxDownloadVpnService.ACTION_CONNECT);
        
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            StartForegroundService(intent);
        }
        else
        {
            StartService(intent);
        }
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        
        if (requestCode == VPN_REQUEST_CODE && resultCode == Result.Ok)
        {
            StartVpn();
        }
    }

    private void OnVpnStateChanged(object sender, bool isRunning)
    {
        RunOnUiThread(UpdateVpnStatus);
    }

    private void UpdateVpnStatus()
    {
        if (txtStatus == null || btnStartVpn == null) return;

        var isRunning = XboxDownload.Android.Services.XboxDownloadVpnService.IsRunning;
        
        txtStatus.Text = isRunning ? "VPN 运行中" : "VPN 已停止";
        txtStatus.SetTextColor(isRunning ? Android.Graphics.Color.Green : Android.Graphics.Color.Red);
        btnStartVpn.Text = isRunning ? "停止 VPN" : "启动 VPN";
    }

    protected override void OnDestroy()
    {
        XboxDownload.Android.Services.XboxDownloadVpnService.StateChanged -= OnVpnStateChanged;
        base.OnDestroy();
    }
}
