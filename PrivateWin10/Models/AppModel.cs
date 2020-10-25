using MiscHelpers;
using PrivateAPI;
using PrivateWin10.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PrivateWin10
{
    public class AppModel
    {
        private static AppModel mInstance = null;
        public static AppModel GetInstance()
        {
            if (mInstance == null)
                mInstance = new AppModel();
            return mInstance;
        }

        public ObservableCollection<AppPkg> Apps { get; set; }

        public AppModel()
        {
            Apps = new ObservableCollection<AppPkg>();
        }

        public class AppPkg : ContentControl
        {
            public string Value { get; set; }
            public string Group { get; set; }
        }

        private Dictionary<string, UwpFunc.AppInfo> AppPackages = new Dictionary<string, UwpFunc.AppInfo>();
        DateTime LastAppReload = DateTime.Now;

        public UwpFunc.AppInfo GetAppPkgBySid(string SID)
        {
            UwpFunc.AppInfo AppContainer = null;
            if (AppPackages.Count == 0 || (!AppPackages.TryGetValue(SID, out AppContainer) && (DateTime.Now - LastAppReload).TotalMilliseconds > 5*1000))
            {
                AppPackages = App.client.GetAllAppPkgs();
                AppPackages.TryGetValue(SID, out AppContainer);
            }
            return AppContainer;
        }

        public void Reload()
        {
            Apps.Clear();

            AppPackages = App.client.GetAllAppPkgs();
            if (AppPackages != null)
            {
                foreach (var AppPkg in AppPackages.Values)
                {
                    Apps.Add(new AppPkg()
                    {
                        Content = AppPkg.Name + " (" + AppPkg.ID + ")",
                        Value = AppPkg.SID,
                        Group = Translate.fmt("lbl_known")
                    });
                }

                //Apps.Add(new AppPkg() { Content = Translate.fmt("app_reload"), Value = null });
            }
        }

        public IEnumerable GetApps()
        {
            if (AppPackages.Count == 0 || (DateTime.Now - LastAppReload).TotalMilliseconds > 5*1000)
                Reload();

            ListCollectionView lcv = new ListCollectionView(Apps);
            lcv.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
            lcv.SortDescriptions.Add(new SortDescription("Content", ListSortDirection.Ascending));
            return lcv;
        }
    }
}
