using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PrivateWin10
{
    public class EtwAbstractLogger
    {
        public enum EVT_VARIANT_TYPE
        {
            EvtVarTypeNull = 0,
            EvtVarTypeString = 1,
            EvtVarTypeAnsiString = 2,
            EvtVarTypeSByte = 3,
            EvtVarTypeByte = 4,
            EvtVarTypeInt16 = 5,
            EvtVarTypeUInt16 = 6,
            EvtVarTypeInt32 = 7,
            EvtVarTypeUInt32 = 8,
            EvtVarTypeInt64 = 9,
            EvtVarTypeUInt64 = 10,
            EvtVarTypeSingle = 11,
            EvtVarTypeDouble = 12,
            EvtVarTypeBoolean = 13,
            EvtVarTypeBinary = 14,
            EvtVarTypeGuid = 15,
            EvtVarTypeSizeT = 16,
            EvtVarTypeFileTime = 17,
            EvtVarTypeSysTime = 18,
            EvtVarTypeSid = 19,
            EvtVarTypeHexInt32 = 20,
            EvtVarTypeHexInt64 = 21,

            // these types used internally
            EvtVarTypeEvtHandle = 32,
            EvtVarTypeEvtXml = 35

        }

        protected Thread workerThread;
        protected string logName;

        protected void OnEtwEvent(Microsoft.O365.Security.ETW.IEventRecord record)
        {
            OnEtwEvent(record, logName);
        }

        public static void OnEtwEvent(Microsoft.O365.Security.ETW.IEventRecord record, string name)
        {
            // WARNING: this function is called from the worker thread

            string line = "ETW " + name + " id: " + record.Id + "; " + " opcode: " + record.Opcode + "; ";
            foreach (var property in record.Properties)
            {
                line += property.Name + ": ";
                switch ((EVT_VARIANT_TYPE)property.Type)
                {
                    case EVT_VARIANT_TYPE.EvtVarTypeString: line += record.GetUnicodeString(property.Name); break;
                    case EVT_VARIANT_TYPE.EvtVarTypeAnsiString: line += record.GetAnsiString(property.Name); break;
                    case EVT_VARIANT_TYPE.EvtVarTypeByte: line += record.GetUInt8(property.Name); break;
                    case EVT_VARIANT_TYPE.EvtVarTypeUInt16: line += record.GetUInt16(property.Name); break;
                    case EVT_VARIANT_TYPE.EvtVarTypeUInt32: line += record.GetUInt32(property.Name); break;
                    case EVT_VARIANT_TYPE.EvtVarTypeUInt64: line += record.GetUInt64(property.Name); break;
                    case EVT_VARIANT_TYPE.EvtVarTypeSByte: line += record.GetInt8(property.Name); break;
                    case EVT_VARIANT_TYPE.EvtVarTypeInt16: line += record.GetInt16(property.Name); break;
                    case EVT_VARIANT_TYPE.EvtVarTypeInt32: line += record.GetInt32(property.Name); break;
                    case EVT_VARIANT_TYPE.EvtVarTypeInt64: line += record.GetInt64(property.Name); break;
                    case EVT_VARIANT_TYPE.EvtVarTypeGuid: break; // ???
                    default:
                        break;
                }
                line += " (Type " + property.Type + ")";
                line += "; ";
            }
            line += " proc (" + record.ProcessId + ")";

            Console.WriteLine(line);
        }
    }

    public class EtwUserLogger : EtwAbstractLogger, IDisposable
    {
        Microsoft.O365.Security.ETW.UserTrace userTrace;
        Microsoft.O365.Security.ETW.Provider dnsCaptureProvider;

        public EtwUserLogger(string name, Guid guid)
        {
            logName = name;

            userTrace = new Microsoft.O365.Security.ETW.UserTrace("etw_" + name);
            dnsCaptureProvider = new Microsoft.O365.Security.ETW.Provider(guid);
            dnsCaptureProvider.Any = Microsoft.O365.Security.ETW.Provider.AllBitsSet;
            dnsCaptureProvider.OnEvent += OnEtwEvent;
            userTrace.Enable(dnsCaptureProvider);

            workerThread = new Thread(() => { userTrace.Start(); });
            workerThread.Start();
        }

        public void Dispose()
        {
            userTrace.Stop();
            workerThread.Join();
        }
    }

    public class EtwKernelLogger : EtwAbstractLogger, IDisposable
    {
        Microsoft.O365.Security.ETW.KernelTrace kernelTrace;
        Microsoft.O365.Security.ETW.Kernel.NetworkTcpipProvider networkProvider;

        public EtwKernelLogger(string name, Microsoft.O365.Security.ETW.Kernel.NetworkTcpipProvider provider)
        {
            logName = name;

            kernelTrace = new Microsoft.O365.Security.ETW.KernelTrace("etw_" + name);
            networkProvider = provider;
            networkProvider.OnEvent += OnEtwEvent;
            kernelTrace.Enable(networkProvider);

            workerThread = new Thread(() => { kernelTrace.Start(); });
            workerThread.Start();
        }

        public void Dispose()
        {
            kernelTrace.Stop();
            workerThread.Join();
        }
    }
}
