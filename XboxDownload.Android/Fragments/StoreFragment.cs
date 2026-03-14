using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace XboxDownload.Android.Fragments;

public class StoreFragment : Androidx.Fragment.App.Fragment
{
    private TabHost tabHost;
    private EditText editSearch;
    private Button btnSearch;
    private ProgressBar progressBar;

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        return inflater.Inflate(Resource.Layout.FragmentStore, container, false);
    }

    public override void OnViewCreated(View view, Bundle savedInstanceState)
    {
        base.OnViewCreated(view, savedInstanceState);

        tabHost = view.FindViewById<TabHost>(Resource.Id.tabHost);
        editSearch = view.FindViewById<EditText>(Resource.Id.editSearch);
        btnSearch = view.FindViewById<Button>(Resource.Id.btnSearch);
        progressBar = view.FindViewById<ProgressBar>(Resource.Id.progressBar);

        SetupTabs();

        btnSearch.Click += (s, e) =>
        {
            var query = editSearch.Text;
            if (!string.IsNullOrWhiteSpace(query))
            {
                Toast.MakeText(Activity, "搜索: " + query, ToastLength.Short).Show();
            }
        };
    }

    private void SetupTabs()
    {
        tabHost.Setup();

        var tabSpec1 = tabHost.NewTabSpec("Featured");
        tabSpec1.SetContent(Resource.Id.tabFeatured);
        tabSpec1.SetIndicator("精选");
        tabHost.AddTab(tabSpec1);

        var tabSpec2 = tabHost.NewTabSpec("GamePass");
        tabSpec2.SetContent(Resource.Id.tabGamePass);
        tabSpec2.SetIndicator("Game Pass");
        tabHost.AddTab(tabSpec2);
    }
}
