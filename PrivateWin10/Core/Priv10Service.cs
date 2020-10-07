using MiscHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrivateWin10
{
    static class Priv10Service
    {

        static public bool Install(bool start = false)
        {
            try
            {
                string binPathName = "\"" + App.appPath + "\\PrivateService.exe\" -svc";

                var svcConfigInfo = ServiceHelper.GetServiceInfoSafe(App.SvcName);
                if (svcConfigInfo != null)
                {
                    // Note: if teh service path is is wrong re-install the service
                    if (!svcConfigInfo.BinaryPathName.Equals(binPathName, StringComparison.OrdinalIgnoreCase))
                    {
                        Uninstall();
                        svcConfigInfo = null;
                    }
                }

                if (svcConfigInfo == null)
                    ServiceHelper.Install(App.SvcName, App.Title, binPathName);

                ServiceHelper.ChangeStartMode(App.SvcName, ServiceHelper.ServiceBootFlag.AutoStart);

                if (start)
                    ServiceHelper.StartService(App.SvcName);

                return true;
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
            }
            return false;
        }

        static public bool Uninstall()
        {
            try
            {
                if (ServiceHelper.ServiceIsInstalled(App.SvcName))
                {
                    if (ServiceHelper.GetServiceState(App.SvcName) == ServiceHelper.ServiceState.Running)
                        ServiceHelper.StopService(App.SvcName);

                    ServiceHelper.Uninstall(App.SvcName);
                }
                return true;
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
            }
            return false;
        }

        static public bool IsInstalled()
        {
            return ServiceHelper.ServiceIsInstalled(App.SvcName);
        }

        static public bool Startup()
        {
            try
            {
                return ServiceHelper.StartService(App.SvcName);
            }
            catch
            {
                return false;
            }
        }

        static public bool Terminate()
        {
            try
            {
                return ServiceHelper.StopService(App.SvcName);
            }
            catch
            {
                return false;
            }
        }
    }
}
