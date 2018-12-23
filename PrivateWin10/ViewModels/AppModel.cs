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

namespace PrivateWin10.ViewModels
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

        public ObservableCollection<App> Apps { get; set; }

        public AppModel()
        {
            Apps = new ObservableCollection<App>();
        }

        public class App : ContentControl
        {
            public string Value { get; set; }
            public string Groupe { get; set; }
        }

        public IEnumerable GetApps()
        {
            Apps.Clear();
            foreach (AppManager.AppInfo app in PrivateWin10.App.itf.GetAllApps())
            {
                Apps.Add(new App() { Content = app.Name + " (" + app.ID + ")", Value = app.SID, Groupe = Translate.fmt("lbl_known") });
            }

            ListCollectionView lcv = new ListCollectionView(Apps);
            lcv.GroupDescriptions.Add(new PropertyGroupDescription("Groupe"));
            return lcv;
        }
    }
}
