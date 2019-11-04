using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

public class AppLog : IDisposable
{
    static private AppLog mInstance = null;
    public static AppLog GetInstance() { return mInstance; }

    private int mLogLimit = 200;
    private string mLogName = null;
    private static ReaderWriterLockSlim mLocker = new ReaderWriterLockSlim();

    EventLogWatcher mEventWatcher = null;

    public struct LogEntry
    {
        public EventLogEntryType entryType;
        public short categoryID;
        public long eventID;
        public string strMessage;
        public Dictionary<string, string> Params;
        //public byte[] binData;
        public DateTime timeGenerated;

        public void SetData(string[] dataStr)
        {
            strMessage = dataStr[0];
            if (dataStr.Length < 2)
                return;
            
            var keys = dataStr[1].Split('|');
            if (keys.Length != dataStr.Length - 2)
                return;

            Params = new Dictionary<string, string>();
            for (int i = 2; i < dataStr.Length; i++)
                Params.Add(keys[i - 2], dataStr[i]);
        }

        public object[] GetData()
        {
            if (Params == null || Params.Count == 0)
                return new string[] { strMessage };

            string[] dataStr;
            dataStr = new string[Params.Count + 2];
            dataStr[0] = strMessage; // first entry is the message text
            dataStr[1] = string.Join("|", Params.Keys); // second entry is the list of parameter names that follow

            int i = 2;
            foreach (var str in Params.Values)
                dataStr[i++] = str;

            return dataStr;
        }
    }

    private List<LogEntry> mLogList = null;

    public class LogEventArgs : EventArgs
    {
        public LogEntry entry;

        public LogEventArgs(LogEntry entry)
        {
            this.entry = entry;
        }
    };

    public event EventHandler<LogEventArgs> LogEvent;

    public AppLog(string LogName = null)
    {
        mInstance = this;

        if (LogName == null)
            return;
            
        if (EventLog.Exists(LogName))
            mLogName = LogName;
        else if (AdminFunc.IsAdministrator())
        {
            try
            {
                if (!EventLog.SourceExists(LogName))
                    EventLog.CreateEventSource(LogName, LogName);
                mLogName = LogName;
            }
            catch { }
        }
    }

    public void Dispose()
    {
        if (mEventWatcher != null)
        {
            mEventWatcher.EventRecordWritten -= new EventHandler<EventRecordWrittenEventArgs>(OnLogEntry);
            mEventWatcher.Dispose();
            mEventWatcher = null;
        }
    }

    public void EnableLogging()
    {
        mLogList = new List<LogEntry>();

        if (mLogName == null)
            return;

        mEventWatcher = new EventLogWatcher(new EventLogQuery(mLogName, PathType.LogName));
        mEventWatcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(OnLogEntry);
        mEventWatcher.Enabled = true;
    }

    public static void RemoveLog(string LogName)
    {
        try { EventLog.Delete(LogName); } catch { }
        try { EventLog.DeleteEventSource(LogName); } catch { }
    }

    public static void Add(EventLogEntryType entryType, long eventID, short categoryID, string strMessage, Dictionary<string, string> Params = null/*, byte[] binData = null*/)
    {
        Console.WriteLine("Log: " + strMessage);

        if (mInstance != null)
            mInstance.AddLogEntry(entryType, eventID, categoryID, strMessage, Params/*, binData*/);
    }

    private void AddLogEntry(EventLogEntryType entryType, long eventID, short categoryID, string strMessage, Dictionary<string, string> Params = null/*, byte[] binData = null*/)
    {
        LogEntry Entry = new LogEntry();
        Entry.entryType = entryType;
        Entry.categoryID = categoryID;
        Entry.eventID = eventID;
        Entry.strMessage = strMessage;
        Entry.Params = Params;
        //Entry.binData = binData;
        Entry.timeGenerated = DateTime.Now;

        if (mLogName == null)
            AddToLog(Entry);
        else
            EventLog.WriteEvent(mLogName, new EventInstance(Entry.eventID, Entry.categoryID, Entry.entryType), /*Entry.binData,*/ Entry.GetData());
    }

    private void AddToLog(LogEntry Entry)
    {
        LogEvent?.Invoke(this, new LogEventArgs(Entry));

        if (mLogList == null)
            return;

        mLocker.EnterWriteLock();
        mLogList.Add(Entry);
        while (mLogList.Count > mLogLimit)
            mLogList.RemoveAt(0);
        mLocker.ExitWriteLock();
    }

    private void OnLogEntry(object obj, EventRecordWrittenEventArgs arg)
    {
        if (arg.EventRecord == null || arg.EventRecord.Properties.Count == 0)
            return;

        try
        {
            LogEntry Entry = new LogEntry();
            Entry.eventID = arg.EventRecord.Id;
            Entry.categoryID = (short)arg.EventRecord.Task;
            switch (arg.EventRecord.Level.Value)
            {
                case 2:     Entry.entryType = EventLogEntryType.Error; break;
                case 3:     Entry.entryType = EventLogEntryType.Warning; break;
                case 4:     
                default:    Entry.entryType = EventLogEntryType.Information; break;
            }
            Entry.timeGenerated = arg.EventRecord.TimeCreated.Value;
            string[] dataStr = new string[arg.EventRecord.Properties.Count];
            for(int i = 0; i < arg.EventRecord.Properties.Count; i++)
                dataStr[i] = arg.EventRecord.Properties[i].Value.ToString();
            Entry.SetData(dataStr);
            //Entry.binData = 

            AddToLog(Entry);
        }
        catch { }
    }

    public void LoadLog()
    {
        if (mLogName == null)
            return;

        mLocker.EnterWriteLock();
        try
        {
            mLogList = new List<LogEntry>();

            EventLog eventLog = new EventLog(mLogName);
            for(int idx = (eventLog.Entries.Count > mLogLimit ? eventLog.Entries.Count - mLogLimit : 0); idx < eventLog.Entries.Count; idx++)
            {
                EventLogEntry logEntry = eventLog.Entries[idx];

                if (logEntry.ReplacementStrings.Length == 0)
                    continue;

                LogEntry Entry = new LogEntry();
                Entry.eventID = logEntry.InstanceId;
                Entry.categoryID = logEntry.CategoryNumber;
                Entry.entryType = logEntry.EntryType;
                Entry.timeGenerated = logEntry.TimeGenerated;
                Entry.SetData(logEntry.ReplacementStrings);
                //Entry.binData = logEntry.Data;

                mLogList.Add(Entry);
            }
        }
        catch { }
        mLocker.ExitWriteLock();
    }

    public List<LogEntry> GetFullLog()
    {
        List<LogEntry> log = null;
        if (mLogList != null)
        {
            mLocker.EnterReadLock();
            log = new List<LogEntry>(mLogList);
            mLocker.ExitReadLock();
        }
        return log;
    }

    static public long ExceptionLogID = 0;
    static public long ExceptionCategory = 0;

    static public void Exception(Exception ex)
    {
#if DEBUG
        Debugger.Break();
#endif

        var st = new StackTrace();
        var sf = st.GetFrame(1);
        var name = sf.GetMethod().Name;

        String message = "Exception in " + name + ": " + ex.Message;
        Dictionary<string, string> values = new Dictionary<string, string>();
        values.Add("Exception", ex.ToString());
        Add(EventLogEntryType.Error, ExceptionLogID, (short)ExceptionCategory, message, values);
    }

    static public void Debug(string message, params object[] args)
    {
        Console.WriteLine(string.Format(message, args));
    }
}