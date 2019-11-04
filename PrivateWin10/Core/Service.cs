using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PrivateWin10
{
    public class Service : ServiceBase
    {
        public Service(string name)
        {
            CanHandlePowerEvent = true;
            CanHandleSessionChangeEvent = true;
            CanPauseAndContinue = true;
            CanShutdown = true;
            ServiceName = name;

            CanHandleSessionChangeEvent = true;
            CanPauseAndContinue = false;
        }

        public bool Install(bool start = false)
        {
            try
            {
                if (!ServiceHelper.ServiceIsInstalled(ServiceName))
                    ServiceHelper.Install(ServiceName, App.mName, "\"" + App.exePath + "\" -svc");

                ServiceHelper.ChangeStartMode(ServiceName, ServiceHelper.ServiceBootFlag.AutoStart);

                if (start)
                    ServiceHelper.StartService(ServiceName);

                return true;
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
            }
            return false;
        }

        public bool Uninstall()
        {
            try
            {
                if (ServiceHelper.ServiceIsInstalled(ServiceName))
                {
                    if (ServiceHelper.GetServiceStatus(ServiceName) == ServiceHelper.ServiceState.Running)
                        ServiceHelper.StopService(ServiceName);

                    ServiceHelper.Uninstall(ServiceName);
                }
                return true;
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
            }
            return false;
        }

        protected override void OnSessionChange(SessionChangeDescription sesionChangeDescription)
        {
            //com channel_.SessionChanged(sesionChangeDescription.SessionId);
            base.OnSessionChange(sesionChangeDescription);
        }

        protected override void OnStart(string[] args)
        {
            Thread thread = new Thread(new ThreadStart(Run));
            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA); // needed for tweaks
            thread.Start();
        }

        private void Run()
        {
            try
            {
                App.engine.Run();
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
                App.engine.Stop();
            }
            catch { }
            base.OnStop();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public bool IsInstalled()
        {
            return ServiceHelper.ServiceIsInstalled(ServiceName);
        }

        public bool Startup()
        {
            return ServiceHelper.StartService(ServiceName);
        }

        public bool Terminate()
        {
            return ServiceHelper.StopService(ServiceName);
        }
    }
}
