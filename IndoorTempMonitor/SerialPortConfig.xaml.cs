using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using static IndoorTempMonitor.ObjectModel;

namespace IndoorTempMonitor
{
    /// <summary>
    /// Interaction logic for SerialPortConfig.xaml
    /// </summary>
    public partial class SerialPortConfig : UserControl
    {
        private SerialPortConfigurations _configs;

        public SerialPortConfig()
        {
            InitializeComponent();
            DataContext = this;

            _configs = new SerialPortConfigurations();

            ReadConfigFile();
        }

        public static T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        private void ReadConfigFile()
        {
            IniFileHelper ini = new IniFileHelper(AppDomain.CurrentDomain.BaseDirectory + @"\config.ini");
            try
            {
                _configs.PortName = ini.Read("SerialPortConfigurations", "PortName", "COM1");
                _configs.BaudRate = Convert.ToInt32(ini.Read("SerialPortConfigurations", "BaudRate", "9600"));
                _configs.Parity = ParseEnum<Parity>(ini.Read("SerialPortConfigurations", "Parity", "None"));
                _configs.DataBits = Convert.ToInt16(ini.Read("SerialPortConfigurations", "DataBits", "8"));
                _configs.StopBits = ParseEnum<StopBits>(ini.Read("SerialPortConfigurations", "StopBits", "One"));
                _configs.Handshake = ParseEnum<Handshake>(ini.Read("SerialPortConfigurations", "Handshake", "None"));
            }
            finally
            {
                ini = null;
            }
        }

        private void WriteConfigFile()
        {
            IniFileHelper ini = new IniFileHelper(AppDomain.CurrentDomain.BaseDirectory + @"\config.ini");
            try
            {
                ini.Write("SerialPortConfigurations", "PortName", _configs.PortName);
                ini.Write("SerialPortConfigurations", "BaudRate", _configs.BaudRate.ToString());
                ini.Write("SerialPortConfigurations", "Parity", _configs.Parity.ToString());
                ini.Write("SerialPortConfigurations", "DataBits", _configs.DataBits.ToString());
                ini.Write("SerialPortConfigurations", "StopBits", _configs.StopBits.ToString());
                ini.Write("SerialPortConfigurations", "Handshake", _configs.Handshake.ToString());
            }
            finally
            {
                ini = null;
            }
        }

        public List<string> PortNameList
        {
            get
            {
                var PortNameList = new List<string>();
                foreach (var port in SerialPort.GetPortNames())
                {
                    PortNameList.Add(port);
                }
                return PortNameList;
            }
        }
        public int[] BaudRateList
        {
            get
            {
                return new int[]
                {
                    100,300,600,1200,2400,4800,9600,14400,19200,
                    38400,56000,57600,115200,128000,256000,0
                };
            }
        }
        public List<StopBits> StopBitsList
        {
            get
            {
                var StopBitsList = new List<StopBits>();
                StopBitsList.Add(StopBits.None);
                StopBitsList.Add(StopBits.One);
                StopBitsList.Add(StopBits.OnePointFive);
                StopBitsList.Add(StopBits.Two);
                return StopBitsList;
            }
        }
        public List<Parity> ParityList
        {
            get
            {
                var ParityList = new List<Parity>();
                ParityList.Add(Parity.Even);
                ParityList.Add(Parity.Mark);
                ParityList.Add(Parity.None);
                ParityList.Add(Parity.Odd);
                ParityList.Add(Parity.Space);
                return ParityList;
            }
        }
        public int[] DataBitsList
        {
            get
            {
                return new int[] { 5, 6, 7, 8 };
            }
        }
        public List<Handshake> HandshakeList
        {
            get
            {
                var HandshakeList = new List<Handshake>();
                HandshakeList.Add(Handshake.None);
                HandshakeList.Add(Handshake.RequestToSend);
                HandshakeList.Add(Handshake.RequestToSendXOnXOff);
                HandshakeList.Add(Handshake.XOnXOff);
                return HandshakeList;
            }
        }

        public string SelectedPortName
        {
            get
            {
                return _configs.PortName;
            }
            set
            {
                _configs.PortName = value;
            }
        }

        public int SelectedBaudRate
        {
            get
            {
                return _configs.BaudRate;
            }
            set
            {
                _configs.BaudRate = value;
            }
        }

        public StopBits SelectedStopBits
        {
            get
            {
                return _configs.StopBits;
            }
            set
            {
                _configs.StopBits = value;
            }
        }

        public Parity SelectedParity
        {
            get
            {
                return _configs.Parity;
            }
            set
            {
                _configs.Parity = value;
            }
        }

        public int SelectedDataBits
        {
            get
            {
                return _configs.DataBits;
            }
            set
            {
                _configs.DataBits = value;
            }
        }

        public Handshake SelectedHandshake
        {
            get
            {
                return _configs.Handshake;
            }
            set
            {
                _configs.Handshake = value;
            }
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            EventPasser.Instance.ConfigureSerialPortOKClicked(_configs);
            WriteConfigFile();
        }
    }
}