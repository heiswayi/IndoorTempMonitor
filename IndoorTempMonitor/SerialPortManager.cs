using System;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace IndoorTempMonitor
{
    public sealed class SerialPortManager
    {
        private static readonly Lazy<SerialPortManager> lazy = new Lazy<SerialPortManager>(() => new SerialPortManager());
        public static SerialPortManager Instance { get { return lazy.Value; } }

        private SerialPort _serialPort;
        private Thread _readThread;
        private volatile bool _keepReading;

        private SerialPortManager()
        {
            _serialPort = new SerialPort();
            _readThread = null;
            _keepReading = false;
        }

        public event EventHandler<string> OnStatusChanged;
        public event EventHandler<string> OnDataReceived;
        public event EventHandler<bool> OnSerialPortOpened;

        public bool IsOpen { get { return _serialPort.IsOpen; } }

        public void Open(
            string portname = "COM1",
            int baudrate = 9600,
            Parity parity = Parity.None,
            int databits = 8,
            StopBits stopbits = StopBits.One,
            Handshake handshake = Handshake.None)
        {
            Close();

            try
            {
                _serialPort.PortName = portname;
                _serialPort.BaudRate = baudrate;
                _serialPort.Parity = parity;
                _serialPort.DataBits = databits;
                _serialPort.StopBits = stopbits;
                _serialPort.Handshake = handshake;

                _serialPort.ReadTimeout = 50;
                _serialPort.WriteTimeout = 50;

                _serialPort.Open();
                StartReading();
            }
            catch (IOException)
            {
                if (OnStatusChanged != null)
                    OnStatusChanged(this, string.Format("{0} does not exist.", portname));
            }
            catch (UnauthorizedAccessException)
            {
                if (OnStatusChanged != null)
                    OnStatusChanged(this, string.Format("{0} already in use.", portname));
            }
            catch (Exception ex)
            {
                if (OnStatusChanged != null)
                    OnStatusChanged(this, "Exception: " + ex.Message);
            }

            if (_serialPort.IsOpen)
            {
                string sb = StopBits.None.ToString().Substring(0, 1);
                switch (_serialPort.StopBits)
                {
                    case StopBits.One:
                        sb = "1"; break;
                    case StopBits.OnePointFive:
                        sb = "1.5"; break;
                    case StopBits.Two:
                        sb = "2"; break;
                    default:
                        break;
                }

                string p = _serialPort.Parity.ToString().Substring(0, 1);
                string hs = _serialPort.Handshake == Handshake.None ? "No Handshake" : _serialPort.Handshake.ToString();

                if (OnStatusChanged != null)
                    OnStatusChanged(this, string.Format(
                    "Connected to {0}: {1} bps, {2}{3}{4}, {5}.",
                    _serialPort.PortName,
                    _serialPort.BaudRate,
                    _serialPort.DataBits,
                    p,
                    sb,
                    hs));

                if (OnSerialPortOpened != null)
                    OnSerialPortOpened(this, true);
            }
            else
            {
                if (OnStatusChanged != null)
                    OnStatusChanged(this, string.Format(
                    "{0} already in use.",
                    portname));

                if (OnSerialPortOpened != null)
                    OnSerialPortOpened(this, false);
            }
        }

        public void Close()
        {
            StopReading();
            _serialPort.Close();

            if (OnStatusChanged != null)
                OnStatusChanged(this, "Connection closed.");

            if (OnSerialPortOpened != null)
                OnSerialPortOpened(this, false);
        }

        public void SendString(string message)
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Write(message);

                if (OnStatusChanged != null)
                    OnStatusChanged(this, string.Format(
                    "Message sent: {0}",
                    message));
            }
        }

        private void StartReading()
        {
            if (!_keepReading)
            {
                _keepReading = true;
                _readThread = new Thread(ReadPort);
                _readThread.Start();
            }
        }

        private void StopReading()
        {
            if (_keepReading)
            {
                _keepReading = false;
                _readThread.Join();
                _readThread = null;
            }
        }

        private void ReadPort()
        {
            while (_keepReading)
            {
                if (_serialPort.IsOpen)
                {
                    //byte[] readBuffer = new byte[_serialPort.ReadBufferSize + 1];
                    try
                    {
                        //int count = _serialPort.Read(readBuffer, 0, _serialPort.ReadBufferSize);
                        //string data = Encoding.ASCII.GetString(readBuffer, 0, count);
                        string data = _serialPort.ReadLine();

                        if (OnDataReceived != null)
                            OnDataReceived(this, data);
                    }
                    catch (TimeoutException) { }
                }
                else
                {
                    TimeSpan waitTime = new TimeSpan(0, 0, 0, 0, 50);
                    Thread.Sleep(waitTime);
                }
            }
        }
    }
}