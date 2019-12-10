using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PrivateWin10
{
    public class Priv10Service : ServiceBase
    {
        public Priv10Service()
        {
            CanHandlePowerEvent = true;
            CanHandleSessionChangeEvent = true;
            CanPauseAndContinue = true;
            CanShutdown = true;
            ServiceName = App.SvcName;

            CanHandleSessionChangeEvent = true;
            CanPauseAndContinue = false;
        }

        protected override void OnSessionChange(SessionChangeDescription sesionChangeDescription)
        {
            //com channel_.SessionChanged(sesionChangeDescription.SessionId);
            base.OnSessionChange(sesionChangeDescription);
        }

        protected override void OnStart(string[] args)
        {
            App.LogInfo("priv10 Service starting");

            Thread thread = new Thread(new ThreadStart(Run));
            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA); // needed for tweaks
            thread.Start();

            //App.LogInfo("priv10 Service started");
        }

        private void Run()
        {
            try
            {
                App.engine.Run();

                this.Stop();
            }
            catch
            {
                ExitCode = -1;
                Environment.Exit(-1);
            }
        }

        protected override void OnStop()
        {
            try
            {
                App.LogInfo("priv10 Service stopping...");

                App.engine.Stop();

                App.LogInfo("priv10 Service stopped");
            }
            catch { }
            base.OnStop();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        ///////////////////////////////////////////////////
        // Static Helpers

        static public bool Install(bool start = false)
        {
            try
            {
                string binPathName = "\"" + App.exePath + "\" -svc";

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
