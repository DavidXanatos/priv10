using MiscHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrivateAPI
{
    public static class Priv10Logger
    {
        public enum EventIDs : long
        {
            Undefined = 0x0000,

            // generic
            Exception,
            AppError,
            AppWarning,
            AppInfo,

            TweakBegin = 0x0100,
            TweakChanged,
            TweakFixed,
            TweakError,
            TweakEnd = 0x01FF,

            FirewallBegin = 0x0200,
            RuleChanged,
            RuleDeleted,
            RuleAdded,
            //FirewallNewProg
            FirewallEnd = 0x02FF,
        }

        public enum EventFlags : short
        {
            DebugEvents = 0x0100,
            AppLogEntries = 0x0200,
            Notifications = 0x0400, // Show a Notification
            PopUpMessages = 0x0800, // Show a PopUp Message
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Event Logging

        static public void LogCriticalError(string message, params object[] args)
        {
#if DEBUG
            Debugger.Break();
#endif
            LogError("Critical Error: " + message, args);
        }

        static public void LogError(string message, params object[] args)
        {
            AppLog.Add(EventLogEntryType.Error, (long)EventIDs.AppError, (short)EventFlags.AppLogEntries, args.Length == 0 ? message : string.Format(message, args));
        }

        static public void LogError(EventIDs eventID, Dictionary<string, string> Params, EventFlags flags, string message, params object[] args)
        {
            AppLog.Add(EventLogEntryType.Error, (long)eventID, (short)flags, args.Length == 0 ? message : string.Format(message, args), Params);
        }

        static public void LogWarning(string message, params object[] args)
        {
            AppLog.Add(EventLogEntryType.Warning, (long)EventIDs.AppWarning, (short)EventFlags.AppLogEntries, args.Length == 0 ? message : string.Format(message, args));
        }

        static public void LogWarning(EventIDs eventID, Dictionary<string, string> Params, EventFlags flags, string message, params object[] args)
        {
            AppLog.Add(EventLogEntryType.Warning, (long)eventID, (short)flags, args.Length == 0 ? message : string.Format(message, args), Params);
        }

        static public void LogInfo(string message, params object[] args)
        {
            AppLog.Add(EventLogEntryType.Information, (long)EventIDs.AppInfo, (short)EventFlags.AppLogEntries, args.Length == 0 ? message : string.Format(message, args));
        }

        static public void LogInfo(EventIDs eventID, Dictionary<string, string> Params, EventFlags flags, string message, params object[] args)
        {
            AppLog.Add(EventLogEntryType.Information, (long)eventID, (short)flags, args.Length == 0 ? message : string.Format(message, args), Params);
        }
    }
}
