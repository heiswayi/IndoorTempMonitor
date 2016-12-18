using System;
using static IndoorTempMonitor.ObjectModel;

namespace IndoorTempMonitor
{
    public sealed class EventPasser
    {
        private static readonly Lazy<EventPasser> lazy = new Lazy<EventPasser>(() => new EventPasser());
        public static EventPasser Instance { get { return lazy.Value; } }

        public EventPasser()
        {
        }

        public event EventHandler<SerialPortConfigurations> OnSerialPortConfigsUpdated;
        internal void ConfigureSerialPortOKClicked(SerialPortConfigurations _configs)
        {
            if (OnSerialPortConfigsUpdated != null)
                OnSerialPortConfigsUpdated(this, _configs);
        }
    }
}