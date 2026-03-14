using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace XboxDownload.Android.Fragments;

public class SpeedTestFragment : Androidx.Fragment.App.Fragment
{
    private Spinner spinnerImport;
    private Spinner spinnerLocation;
    private Button btnStartTest;
    private TextView txtStatus;

    private string[] importOptions = new string[] { "Akamai", "Xbox CN1", "Xbox CN2", "Xbox App", "PlayStation" };
    private string[] locationOptions = new string[] { "全部", "电信", "联通", "移动", "香港", "日本", "新加坡" };

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        return inflater.Inflate(Resource.Layout.FragmentSpeedTest, container, false);
    }

    public override void OnViewCreated(View view, Bundle savedInstanceState)
    {
        base.OnViewCreated(view, savedInstanceState);

        spinnerImport = view.FindViewById<Spinner>(Resource.Id.spinnerImport);
        spinnerLocation = view.FindViewById<Spinner>(Resource.Id.spinnerLocation);
        btnStartTest = view.FindViewById<Button>(Resource.Id.btnStartTest);
        txtStatus = view.FindViewById<TextView>(Resource.Id.txtStatus);

        spinnerImport.Adapter = new ArrayAdapter<string>(Activity, Android.Resource.Layout.SimpleSpinnerItem, importOptions);
        spinnerLocation.Adapter = new ArrayAdapter<string>(Activity, Android.Resource.Layout.SimpleSpinnerItem, locationOptions);

        btnStartTest.Click += (s, e) =>
        {
            txtStatus.Text = "测速功能开发中...";
            Toast.MakeText(Activity, "测速功能即将上线", ToastLength.Short).Show();
        };
    }
}
