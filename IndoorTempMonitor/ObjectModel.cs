using System;
using System.Collections.Concurrent;
using System.IO.Ports;

namespace IndoorTempMonitor
{
    public class ObjectModel
    {
        public class SerialPortConfigurations
        {
            public string PortName { get; set; }
            public int BaudRate { get; set; }
            public int DataBits { get; set; }
            public StopBits StopBits { get; set; }
            public Parity Parity { get; set; }
            public Handshake Handshake { get; set; }
        }

        public class TemperatureData
        {
            public DateTime Time { get; set; }
            public double Value { get; set; }
        }

        public class FixedSizedQueue<T> : ConcurrentQueue<T>
        {
            private readonly object syncObject = new object();

            public int Size { get; private set; }

            public FixedSizedQueue(int size)
            {
                Size = size;
            }

            public new void Enqueue(T obj)
            {
                base.Enqueue(obj);
                lock (syncObject)
                {
                    while (base.Count > Size)
                    {
                        T outObj;
                        base.TryDequeue(out outObj);
                    }
                }
            }
        }
    }
}