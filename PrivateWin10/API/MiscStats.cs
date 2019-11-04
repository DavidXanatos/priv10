using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrivateWin10
{

    [Serializable()]
    public class RateCounter
    {
        const int AVG_INTERVAL = 3 * 1000;

        public RateCounter()
        {
            ByteRate = 0;
            RateStat = new Queue<DeltaItem>();
            TotalBytes = 0;
            TotalTime = 0;
        }

        public void Update(UInt64 Interval, UInt64 AddDelta)
        {
            while (TotalTime > AVG_INTERVAL && RateStat.Count > 0)
            {
                DeltaItem Front = RateStat.Dequeue();
                TotalTime -= Front.Interval;
                TotalBytes -= Front.Bytes;
            }

            DeltaItem Back = new DeltaItem() { Interval = Interval, Bytes = AddDelta };
            TotalTime += Back.Interval;
            TotalBytes += Back.Bytes;
            RateStat.Enqueue(Back);

            UInt64 totalTime = TotalTime > 0 ? TotalTime : Interval;
            if (totalTime < AVG_INTERVAL / 2)
                totalTime = AVG_INTERVAL;
            ByteRate = TotalBytes * 1000 / totalTime;
        }

        [Serializable()]
        struct DeltaItem
        {
            public UInt64 Interval;
            public UInt64 Bytes;
        };

        public UInt64 ByteRate;
        Queue<DeltaItem> RateStat;
        UInt64 TotalBytes;
        UInt64 TotalTime;
    }

    [Serializable()]
    public class Delta64
    {
        public Delta64()
        {
            Initialized = false;
            Value = 0;
            Delta = 0;
        }

        public void Update(UInt64 New)
        {
            if (!Initialized)
                Initialized = true;
            else if (New < Value) // some counters may reset...
                Delta = 0;
            else
                Delta = New - Value;
            Value = New;
        }

        public UInt64 Value;
        public UInt64 Delta;
        private bool Initialized;
    }


    [Serializable()]
    public class NetworkStats
    {
        public UInt64 SentBytes;
        public Delta64 SentDelta;
        public RateCounter UploadRate;

        public UInt64 ReceivedBytes;
        public Delta64 ReceivedDelta;
        public RateCounter DownloadRate;

        public NetworkStats()
        {
            SentBytes = 0;
            SentDelta = new Delta64();
            UploadRate = new RateCounter();

            ReceivedBytes = 0;
            ReceivedDelta = new Delta64();
            DownloadRate = new RateCounter();
        }

        public void Update(UInt64 Interval)
        {
            SentDelta.Update(SentBytes);
            ReceivedDelta.Update(ReceivedBytes);

            UploadRate.Update(Interval, SentDelta.Delta);
            DownloadRate.Update(Interval, ReceivedDelta.Delta);
        }
    }
}
