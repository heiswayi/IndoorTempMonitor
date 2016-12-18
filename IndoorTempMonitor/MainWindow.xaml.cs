using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using static IndoorTempMonitor.ObjectModel;

namespace IndoorTempMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Private members
        private double _minTemp, _maxTemp, _avgTemp;
        private List<TemperatureData> _tempDataCollection;
        private SerialPortConfigurations _serialPortConfigs;
        private bool _addingData;
        private int _stabilityCount, _secondCount;
        #endregion Private members

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            StopSerialPort.IsEnabled = false;

            Closing += (obj, e) =>
            {
                if (SerialPortManager.Instance.IsOpen)
                    SerialPortManager.Instance.Close();
            };

            // Create clock
            DispatcherTimer clock = new DispatcherTimer(DispatcherPriority.Background);
            clock.Interval = TimeSpan.FromSeconds(1);
            clock.IsEnabled = true;
            clock.Tick += (obj, e) =>
            {
                ClockText.Text = DateTime.Now.ToLocalTime().ToString();
            };

            // Subscribe SerialPort events
            SerialPortManager.Instance.OnDataReceived += (obj, e) =>
            {
                double temp;
                try
                {
                    temp = Convert.ToDouble(e);
                }
                catch
                {
                    temp = 0;
                }

                if (_stabilityCount < 5)
                {
                    _stabilityCount++;
                    return;
                }

                if (_stabilityCount == 5 && _secondCount == 60)
                {
                    ExportDataToCSV(DateTime.Now, temp);
                    _secondCount = 0;
                }
                else
                {
                    _secondCount++;
                }

                App.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    MainTempText.Text = string.Format("{0} °C", Math.Round(temp, 2));
                    UpdateMainTempBoxColor(temp);
                    UpdateMinMaxTemp(temp);
                    AddToDataCollection(DateTime.Now, temp);
                }));
            };
            SerialPortManager.Instance.OnSerialPortOpened += (obj, e) =>
            {
                if (e == true)
                {
                    StartSerialPort.IsEnabled = false;
                    StopSerialPort.IsEnabled = true;
                }
                else
                {
                    StartSerialPort.IsEnabled = true;
                    StopSerialPort.IsEnabled = false;
                }
            };

            // Subscribe events from EventPasser
            EventPasser.Instance.OnSerialPortConfigsUpdated += (obj, e) =>
            {
                _serialPortConfigs = e;
                popupSerialPortConfig.IsOpen = false;
                ConfigureSerialPort.IsEnabled = true;
            };
            ReadConfigFile();

            // OxyPlot
            Plot1 = CreatePlotModel();
        }
        #endregion Constructor

        #region Binding properties
        public PlotModel Plot1 { get; set; }
        public Collection<TemperatureData> DataCollection { get; set; }
        #endregion Binding properties

        #region MenuItem click events
        private void ShowAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Indoor Temperature Monitor v1.0\nCreated by Heiswayi Nrird.", "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowHideChart_Click(object sender, RoutedEventArgs e)
        {
            if (ChartGrid.Visibility == Visibility.Collapsed)
            {
                this.Height = 600;
                ChartGrid.Visibility = Visibility.Visible;
                ShowHideChart.Header = "Hide Live Chart";
            }
            else
            {
                this.Height = 400;
                ChartGrid.Visibility = Visibility.Collapsed;
                ShowHideChart.Header = "Show Live Chart";
            }
        }

        private void ConfigureSerialPort_Click(object sender, RoutedEventArgs e)
        {
            if (!popupSerialPortConfig.IsOpen)
            {
                popupSerialPortConfig.IsOpen = true;
                ConfigureSerialPort.IsEnabled = false;
            }
        }

        private void StartSerialPort_Click(object sender, RoutedEventArgs e)
        {
            if (SerialPortManager.Instance.IsOpen)
                SerialPortManager.Instance.Close();

            _stabilityCount = 0;
            _secondCount = 0;

            if (_tempDataCollection != null) _tempDataCollection.Clear();

            if (_serialPortConfigs != null)
            {
                string outputFolder = string.Format(@"{0}\templog", AppDomain.CurrentDomain.BaseDirectory);
                string filePath = string.Format(@"{0}\{1}.csv", outputFolder, DateTime.Now.ToString("yyyy-MM-dd"));

                try
                {
                    if (File.Exists(filePath)) File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Exception: " + ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                SerialPortManager.Instance.Open(_serialPortConfigs.PortName, _serialPortConfigs.BaudRate, _serialPortConfigs.Parity, _serialPortConfigs.DataBits, _serialPortConfigs.StopBits, _serialPortConfigs.Handshake);
            }
        }

        private void StopSerialPort_Click(object sender, RoutedEventArgs e)
        {
            SerialPortManager.Instance.Close();
        }
        #endregion MenuItem click events

        #region Private methods
        private void ExportDataToCSV(DateTime datetime, double temp)
        {
            try
            {
                string outputFolder = string.Format(@"{0}\templog", AppDomain.CurrentDomain.BaseDirectory);
                string filePath = string.Format(@"{0}\{1}.csv", outputFolder, DateTime.Now.ToString("yyyy-MM-dd"));
                string appendData = string.Format("{0},{1}", datetime.ToString("yyyy-MM-dd HH:mm:ss"), Math.Round(temp, 2));

                Directory.CreateDirectory(outputFolder);

                if (!File.Exists(filePath))
                {
                    string header = "Time,Temperature (°C)";
                    File.WriteAllText(filePath, header + Environment.NewLine);
                }

                File.AppendAllText(filePath, appendData + Environment.NewLine);
            }
            catch
            {
                return;
            }
        }

        private void ReadConfigFile()
        {
            if (_serialPortConfigs == null)
                _serialPortConfigs = new SerialPortConfigurations();

            IniFileHelper ini = new IniFileHelper(AppDomain.CurrentDomain.BaseDirectory + @"\config.ini");
            try
            {
                _serialPortConfigs.PortName = ini.Read("SerialPortConfigurations", "PortName", "COM1");
                _serialPortConfigs.BaudRate = Convert.ToInt32(ini.Read("SerialPortConfigurations", "BaudRate", "9600"));
                _serialPortConfigs.Parity = ParseEnum<Parity>(ini.Read("SerialPortConfigurations", "Parity", "None"));
                _serialPortConfigs.DataBits = Convert.ToInt16(ini.Read("SerialPortConfigurations", "DataBits", "8"));
                _serialPortConfigs.StopBits = ParseEnum<StopBits>(ini.Read("SerialPortConfigurations", "StopBits", "One"));
                _serialPortConfigs.Handshake = ParseEnum<Handshake>(ini.Read("SerialPortConfigurations", "Handshake", "None"));
            }
            finally
            {
                ini = null;
            }
        }

        private void AddToDataCollection(DateTime now, double temp)
        {
            if (_tempDataCollection == null)
                _tempDataCollection = new List<TemperatureData>();

            if (!_addingData)
            {
                _addingData = true;

                _tempDataCollection.Add(new TemperatureData() { Time = now, Value = temp });

                _minTemp = _tempDataCollection.Min(x => x.Value);
                _maxTemp = _tempDataCollection.Max(x => x.Value);
                _avgTemp = _tempDataCollection.Average(x => x.Value);

                if (DataCollection.Count() > 60)
                    DataCollection.RemoveAt(0);

                DataCollection.Add(new TemperatureData() { Time = now, Value = temp });
                Plot1.InvalidatePlot(true);

                _addingData = false;
            }
        }

        private void UpdateMinMaxTemp(double temp)
        {
            MinTempText.Text = string.Format("Min {0} °C", Math.Round(_minTemp, 2));
            MaxTempText.Text = string.Format("Max {0} °C", Math.Round(_maxTemp, 2));
            AvgTempText.Text = string.Format("Avg. {0} °C", Math.Round(_avgTemp, 2));
        }

        private void UpdateMainTempBoxColor(double temp)
        {
            // < 0 : 6C00D0
            // 0 - 10 : 0000FE
            // 11 - 16 : 0093DD
            // 17 - 22 : 5ADB04
            // 18 - 25 : FFFF00
            // 26 - 28 : FFDD00
            // 29 - 33 : FE9900
            // 34 - 40 : FE5900
            // 40+ : FE0000

            if (temp < 0)
                MainTempBox.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#6C00D0"));
            else if (temp >= 0 && temp <= 10)
                MainTempBox.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#0000FE"));
            else if (temp > 10 && temp <= 16)
                MainTempBox.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#0093DD"));
            else if (temp > 16 && temp <= 22)
                MainTempBox.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#5ADB04"));
            else if (temp > 22 && temp <= 25)
                MainTempBox.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFFF00"));
            else if (temp > 25 && temp <= 28)
                MainTempBox.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFDD00"));
            else if (temp > 28 && temp <= 33)
                MainTempBox.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FE9900"));
            else if (temp > 33 && temp <= 40)
                MainTempBox.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FE5900"));
            else if (temp > 40)
                MainTempBox.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FE0000"));
            else
                MainTempBox.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFFFFF"));
        }

        private PlotModel CreatePlotModel()
        {
            var tmp = new PlotModel { Title = "Live Graph" };

            // X-axis
            tmp.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.Dot,
                Title = "Time",
                IntervalType = DateTimeIntervalType.Seconds,
                MinorIntervalType = DateTimeIntervalType.Seconds,
                IntervalLength = 60,
                MajorTickSize = 5,
                StringFormat = "hh:mm:ss tt"
            });

            // Y-axis
            tmp.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Temperature (°C)",
                MinimumMajorStep = 10
            });

            DataCollection = new Collection<TemperatureData>();

            var s1 = new LineSeries
            {
                Title = "Temperature",
                StrokeThickness = 1,
                MarkerSize = 3,
                ItemsSource = DataCollection,
                DataFieldX = "Time",
                DataFieldY = "Value",
                MarkerStroke = OxyColors.Red,
                MarkerType = MarkerType.Circle,
                Color = OxyColors.OrangeRed
            };

            tmp.Series.Add(s1);
            return tmp;
        }
        #endregion Private methods

        #region Helper functions
        public static T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion Helper functions
    }
}