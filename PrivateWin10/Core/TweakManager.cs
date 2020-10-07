using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Threading;
using System.Xml;
using MiscHelpers;
using PrivateAPI;
using TweakEngine;

namespace PrivateWin10
{
    public class TweakManager: TweakList
    {
        DispatcherTimer Timer;
        UInt64 NextTweakCheck = 0;
        UInt64 LastSaveTime = MiscFunc.GetTickCount64();

        [Serializable()]
        public class TweakEventArgs : EventArgs
        {
            public enum State
            {
                eNone,
                eChanged,
                eRestored
            };

            public State state;
            public Tweak tweak;
        }

        public event EventHandler<TweakEventArgs> TweakChanged;

        public TweakManager()
        {
            Load(App.dataPath + @"\Tweaks.xml");

            Timer = new DispatcherTimer();
            Timer.Tick += new EventHandler(OnTimerTick);
            Timer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            Timer.Start();

            NextTweakCheck = 0;
        }

        public void Store()
        {
            Store(App.dataPath + @"\Tweaks.xml");
        }

        protected void OnTimerTick(object sender, EventArgs e)
        {
            if (NextTweakCheck <= MiscFunc.GetCurTick())
            {
                NextTweakCheck = MiscFunc.GetCurTick() + (UInt64)App.GetConfigInt("TweakGuard", "CheckInterval", 15 * 60) * 1000;

                if (App.GetConfigInt("TweakGuard", "AutoCheck", 1) != 0)
                    TestTweaks(false, App.GetConfigInt("TweakGuard", "AutoFix", 0) != 0);
            }


            if (MiscFunc.GetTickCount64() - LastSaveTime > 15 * 60 * 1000) // every 15 minutes
            {
                LastSaveTime = MiscFunc.GetTickCount64();
                Store();
            }
        }

        public bool ApplyTweak(Tweak tweak, bool? byUser = null)
        {
            if(byUser != null)
                tweak.State = byUser == true ? TweakList.Tweak.States.Sellected : TweakList.Tweak.States.SelGroupe;
            tweak.Status = true;

            if (!tweak.IsAvailable())
                return false;

            bool success;
            if (AdminFunc.IsAdministrator() || tweak.usrLevel)
                success = TweakTools.ApplyTweak(tweak);
            else
                success = App.client.ApplyTweak(tweak);

            TweakChanged?.Invoke(this, new TweakEventArgs() { tweak = tweak });
            return success;
        }

        public bool UndoTweak(Tweak tweak)
        {
            tweak.State = TweakList.Tweak.States.Unsellected;
            tweak.Status = false;

            if (!tweak.IsAvailable())
                return false;

            bool success;
            if (AdminFunc.IsAdministrator() || tweak.usrLevel)
                success = TweakTools.UndoTweak(tweak);
            else
                success = App.client.UndoTweak(tweak);
            TweakChanged?.Invoke(this, new TweakEventArgs() { tweak = tweak });
            return success;
        }

        public void TestTweaks(bool bAll = true, bool fixChanged = false)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            //foreach (Tweak tweak in TweakList.Values)
            foreach (Tweak tweak in GetAllTweaks())
            {
                if(bAll || tweak.State != Tweak.States.Unsellected)
                    TestTweak(tweak, fixChanged);
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            AppLog.Debug("TestAllTweaks took: " + elapsedMs + "ms");
        }

        public bool TestTweak(Tweak tweak, bool fixChanged = false)
        {
            if (!tweak.IsAvailable())
                return false;

            bool status;
            if (AdminFunc.IsAdministrator() || tweak.usrLevel || !App.client.IsConnected())
                status = TweakTools.TestTweak(tweak);
            else
                status = App.client.TestTweak(tweak);

            if (tweak.Status != status)
            {
                tweak.Status = status;
                tweak.LastChangeTime = DateTime.Now;

                Dictionary<string, string> Params = new Dictionary<string, string>();
                Params.Add("Name", tweak.Name);
                Params.Add("Group", tweak.Group);
                Params.Add("Category", tweak.Category);

                if (tweak.Status == false && tweak.State != Tweak.States.Unsellected)
                {
                    TweakEventArgs.State state = TweakEventArgs.State.eChanged;

                    if (fixChanged == true && tweak.FixFailed == false)
                    {
                        ApplyTweak(tweak);

                        if (TestTweak(tweak, false) != true)
                        {
                            tweak.FixFailed = true;
                            Priv10Logger.LogError(Priv10Logger.EventIDs.TweakError, Params, Priv10Logger.EventFlags.Notifications, Translate.fmt("msg_tweak_stuck", tweak.Name, tweak.Group));
                        }
                        else
                        {
                            state = TweakEventArgs.State.eRestored;
                            tweak.FixedCount++;
                            Priv10Logger.LogInfo(Priv10Logger.EventIDs.TweakFixed, Params, Priv10Logger.EventFlags.Notifications, Translate.fmt("msg_tweak_fixed", tweak.Name, tweak.Group));
                        }
                    }
                    else
                    {
                        Priv10Logger.LogWarning(Priv10Logger.EventIDs.TweakChanged, Params, Priv10Logger.EventFlags.Notifications, Translate.fmt("msg_tweak_un_done", tweak.Name, tweak.Group));
                    }

                    TweakChanged?.Invoke(this, new TweakEventArgs() { tweak = tweak, state = state });
                }
            }
            return status;
        }
    }
}
