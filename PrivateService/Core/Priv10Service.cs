using MiscHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PrivateService;
using PrivateAPI;

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
            Priv10Logger.LogInfo("priv10 Service starting");

            Thread thread = new Thread(new ThreadStart(Run));
            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA); // needed for tweaks
            thread.Start();

            //Priv10Logger.LogInfo("priv10 Service started");
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
                Priv10Logger.LogInfo("priv10 Service stopping...");

                App.engine.Stop();

                Priv10Logger.LogInfo("priv10 Service stopped");
            }
            catch { }
            base.OnStop();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
        
    }
}
