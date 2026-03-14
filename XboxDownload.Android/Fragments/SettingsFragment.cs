using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace XboxDownload.Android.Fragments;

public class SettingsFragment : Androidx.Fragment.App.Fragment
{
    private EditText editUpstreamDns;
    private TextView txtVersion;

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        return inflater.Inflate(Resource.Layout.FragmentSettings, container, false);
    }

    public override void OnViewCreated(View view, Bundle savedInstanceState)
    {
        base.OnViewCreated(view, savedInstanceState);

        editUpstreamDns = view.FindViewById<EditText>(Resource.Id.editUpstreamDns);
        txtVersion = view.FindViewById<TextView>(Resource.Id.txtVersion);

        editUpstreamDns.Text = XboxDownload.Android.Services.XboxDownloadVpnService.UpstreamDns;
        
        try
        {
            var packageInfo = Activity.PackageManager.GetPackageInfo(Activity.PackageName, 0);
            txtVersion.Text = "版本: " + packageInfo.VersionName;
        }
        catch
        {
            txtVersion.Text = "版本: 1.0.0";
        }

        editUpstreamDns.FocusChange += (s, e) =>
        {
            if (!e.HasFocus)
            {
                var dns = editUpstreamDns.Text;
                if (!string.IsNullOrWhiteSpace(dns))
                {
                    XboxDownload.Android.Services.XboxDownloadVpnService.UpstreamDns = dns;
                    Toast.MakeText(Activity, "DNS设置已保存", ToastLength.Short).Show();
                }
            }
        };
    }
}
