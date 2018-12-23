using PrivateWin10.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PrivateWin10.ViewModels
{
    public class ServiceModel
    {
        private static ServiceModel mInstance = null;
        public static ServiceModel GetInstance()
        {
            if (mInstance == null)
                mInstance = new ServiceModel();
            return mInstance;
        }

        public ObservableCollection<Service> Services { get; set; }

        public ServiceModel()
        {
            Services = new ObservableCollection<Service>();

            Services.Add(new Service() { Content = Translate.fmt("svc_all"), Value="*", Groupe = Translate.fmt("lbl_selec") });

            foreach (ServiceController svc in ServiceController.GetServices().OrderBy(x => x.DisplayName))
                Services.Add(new Service() { Value = svc.ServiceName, Content = svc.DisplayName + " (" + svc.ServiceName + ")", Groupe = Translate.fmt("lbl_known") });
        }

        public class Service : ContentControl
        {
            public string Value { get; set; }
            public string Groupe { get; set; }
        }

        public IEnumerable GetServices()
        {
            ListCollectionView lcv = new ListCollectionView(Services);
            lcv.GroupDescriptions.Add(new PropertyGroupDescription("Groupe"));
            return lcv;
        }
    }
}
