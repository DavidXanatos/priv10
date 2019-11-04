using PrivateWin10.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public IEnumerable GetApps()
        {
            Apps.Clear();

            if (App.PkgMgr != null)
            {
                foreach (AppManager.AppInfo app in App.PkgMgr.GetAllApps())
                    Apps.Add(new AppPkg() { Content = app.Name + " (" + app.ID + ")", Value = app.SID, Group = Translate.fmt("lbl_known") });

                Apps.Add(new AppPkg() { Content = Translate.fmt("app_reload"), Value = null });
            }

            ListCollectionView lcv = new ListCollectionView(Apps);
            lcv.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
            return lcv;
        }
    }
}
