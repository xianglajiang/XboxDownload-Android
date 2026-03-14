using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace XboxDownload.Android.Fragments;

public class HostFragment : Androidx.Fragment.App.Fragment
{
    private ListView listViewHosts;
    private Button btnAdd;
    private Button btnDelete;
    private Button btnImport;

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        return inflater.Inflate(Resource.Layout.FragmentHost, container, false);
    }

    public override void OnViewCreated(View view, Bundle savedInstanceState)
    {
        base.OnViewCreated(view, savedInstanceState);

        listViewHosts = view.FindViewById<ListView>(Resource.Id.listViewHosts);
        btnAdd = view.FindViewById<Button>(Resource.Id.btnAdd);
        btnDelete = view.FindViewById<Button>(Resource.Id.btnDelete);
        btnImport = view.FindViewById<Button>(Resource.Id.btnImport);

        btnAdd.Click += (s, e) =>
        {
            Toast.MakeText(Activity, "添加 Host", ToastLength.Short).Show();
        };

        btnDelete.Click += (s, e) =>
        {
            Toast.MakeText(Activity, "删除 Host", ToastLength.Short).Show();
        };

        btnImport.Click += (s, e) =>
        {
            var clipboard = (Android.Content.ClipboardManager)Activity.GetSystemService(Context.ClipboardService);
            var text = clipboard.Text;
            if (!string.IsNullOrEmpty(text))
            {
                Toast.MakeText(Activity, "导入成功", ToastLength.Short).Show();
            }
        };
    }
}
