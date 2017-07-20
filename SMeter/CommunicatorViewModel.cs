using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.InteropServices;
using System.Management;
using System.IO;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Threading;
using ConfigurationFile;
using hid;


namespace SMeter
{


    /*
     *  The ViewModel 
     */
    public class CommunicatorViewModel : INotifyPropertyChanged
    {
        private Communicator communicator;
        private ConfigFile config;
        DispatcherTimer timer;
        private DateTime ConnectedTimestamp = DateTime.Now;
        public string ActivityLogTxt { get; private set; }
        private Int16[] _calibration = new Int16[14];
        private ushort _Pid;
        private ushort _Vid;
        private int _WindowPositionX;
        private int _WindowPositionY;
        public event PropertyChangedEventHandler PropertyChanged;

        public CommunicatorViewModel()
        {
            config = new ConfigFile("config.xml");
            _WindowPositionX = config.PositionX;
            _WindowPositionY = config.PositionY;
            
            communicator = new Communicator();
            communicator.HidUtil.RaiseDeviceAddedEvent += DeviceAddedEventHandler;
            communicator.HidUtil.RaiseDeviceRemovedEvent += DeviceRemovedEventHandler;
            communicator.HidUtil.RaiseConnectionStatusChangedEvent += ConnectionStatusChangedHandler;
            _Vid = config.VendorId;
            _Pid = config.ProductId;
            communicator.Vid = _Vid;
            communicator.Pid = _Pid;

            //Configure and start timer
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(50);
            timer.Tick += TimerTickHandler;
            timer.Start();

            WriteLog("Program started", true);
        }

        //Destructor
        ~CommunicatorViewModel()
        {
            //Save data to config file
            config.PositionX = _WindowPositionX;
            config.PositionY = _WindowPositionY;
        }

        /*
         * Local function definitions
         */

        // Add a line to the activity log text box
        void WriteLog(string message, bool clear)
        {
            // Replace content
            if (clear)
            {
                ActivityLogTxt = string.Format("{0}: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), message);
            }
            // Add new line
            else
            {
                ActivityLogTxt += Environment.NewLine + string.Format("{0}: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), message);
            }
        }
 
        public void SavePidVid()
        {
            if((_Pid != communicator.Pid) || (_Vid != communicator.Vid))
            {
                config.ProductId = _Pid;
                config.VendorId = _Vid;
                communicator.Pid = _Pid;
                communicator.Vid = _Vid;
                string log = string.Format("New PID/VID saved and applied (VID=0x{0:X4} PID=0x{1:X4})", _Vid, _Pid);
                WriteLog(log, false);
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
            else
            {
                WriteLog("Nothing to save", false);
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public void ResetPidVid()
        {
            if ((_Pid != communicator.Pid) || (_Vid != communicator.Vid))
            {
                _Pid = communicator.Pid;
                _Vid = communicator.Vid;
                PropertyChanged(this, new PropertyChangedEventArgs("VidTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("PidTxt"));
                WriteLog("PID/VID reset", false);
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
            else
            {
                WriteLog("Nothing to reset", false);
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public void ResetCalibration()
        {
            for(int i=0; i<_calibration.Length; ++i)
            {
                _calibration[i] = communicator.CalibrationValues[i];
            }
            WriteLog("Calibration resetted", false);
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration00Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration01Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration02Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration03Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration04Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration05Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration06Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration07Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration08Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration09Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration10Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration11Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration12Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration13Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public void SaveCalibration()
        {
            for (int i = 0; i < _calibration.Length; ++i)
            {
                if(_calibration[i] != communicator.CalibrationValues[i])
                {
                    communicator.ScheduleCommand(new Communicator.UsbCommand(0x77, (byte)i, _calibration[i]));
                }
                    
            }
            WriteLog("Calibration saved", false);
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration00Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration01Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration02Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration03Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration04Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration05Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration06Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration07Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration08Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration09Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration10Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration11Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration12Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration13Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public ICommand SavePidVidClick
        {
            get
            {
                return new UiCommand(this.SavePidVid, communicator.RequestValid);
            }
        }

        public ICommand ResetPidVidClick
        {
            get
            {
                return new UiCommand(this.ResetPidVid, communicator.RequestValid);
            }
        }

        public ICommand ResetCalibrationClick
        {
            get
            {
                return new UiCommand(this.ResetCalibration, communicator.RequestValid);
            }
        }

        public ICommand SaveCalibrationClick
        {
            get
            {
                return new UiCommand(this.SaveCalibration, communicator.RequestValid);
            }
        }

        public string UserInterfaceColor
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus == HidUtility.UsbConnectionStatus.Connected)
                    return "Black";
                else
                    return "Gray";
            }
        }

        public void TimerTickHandler(object sender, EventArgs e)
        {
            if (PropertyChanged != null)
            {
                if (communicator.NewDataAvailable)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("CurrentMeasurementAdc"));
                    PropertyChanged(this, new PropertyChangedEventArgs("CurrentMeasurementAdcTxt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("CurrentMeasurement"));
                    PropertyChanged(this, new PropertyChangedEventArgs("CurrentMeasurementTxt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("CurrentMeasurementVTxt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("CurrentMeasurementPTxt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("CurrentMeasurementSTxt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("BarColor"));
                }

                //Update these in any case
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("ConnectionStatusTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("UptimeTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("TxSuccessfulTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("TxFailedTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("RxSuccessfulTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("RxFailedTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("TxSpeedTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("RxSpeedTxt"));
            }
        }

        public void DeviceAddedEventHandler(object sender, Device dev)
        {
            WriteLog("Device added: " + dev.ToString(), false);
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("DeviceListTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public void DeviceRemovedEventHandler(object sender, Device dev)
        {
            WriteLog("Device removed: " + dev.ToString(), false);
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("DeviceListTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }

        }

        public void ConnectionStatusChangedHandler(object sender, HidUtility.ConnectionStatusEventArgs e)
        {
            WriteLog("Connection status changed to: " + e.ToString(), false);
            switch (e.ConnectionStatus)
            {
                case HidUtility.UsbConnectionStatus.Connected:
                    ConnectedTimestamp = DateTime.Now;
                    break;
                case HidUtility.UsbConnectionStatus.Disconnected:
                    break;
                case HidUtility.UsbConnectionStatus.NotWorking:
                    break;
            }
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("ConnectionStatusTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("UptimeTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("UserInterfaceColor"));
            }
        }

        public string DeviceListTxt
        {
            get
            {
                string txt = "";
                foreach (Device dev in communicator.HidUtil.DeviceList)
                {
                    string devString = string.Format("VID=0x{0:X4} PID=0x{1:X4}: {2} ({3})", dev.Vid, dev.Pid, dev.Caption, dev.Manufacturer);
                    txt += devString + Environment.NewLine;
                }
                return txt.TrimEnd();
            }
        }

        // Try to convert a (hexadecimal) string to an unsigned 16-bit integer
        // Return 0 if the conversion fails
        // This function is used to parse the PID and VID text boxes
        private ushort ParseHex(string input)
        {
            input = input.ToLower();
            if (input.Length >= 2)
            {
                if (input.Substring(0, 2) == "0x")
                {
                    input = input.Substring(2);
                }
            }
            try
            {
                ushort value = ushort.Parse(input, System.Globalization.NumberStyles.HexNumber);
                return value;
            }
            catch
            {
                return 0;
            }
        }

        public string VidTxt
        {
            get
            {
                return string.Format("0x{0:X4}", _Vid);
            }
            set
            {
                _Vid = ParseHex(value);
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public string PidTxt
        {
            get
            {
                return string.Format("0x{0:X4}", _Pid);
            }
            set
            {
                _Pid = ParseHex(value);
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public string ConnectionStatusTxt
        {
            get
            {
                return string.Format("Connection Status: {0}", communicator.HidUtil.ConnectionStatus.ToString());
            }
        }

        public string UptimeTxt
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus == HidUtility.UsbConnectionStatus.Connected)
                {
                    //Save time elapsed since the device was connected
                    TimeSpan uptime = DateTime.Now - ConnectedTimestamp;
                    //Return uptime as string
                    return string.Format("Uptime: {0}", uptime.ToString(@"hh\:mm\:ss\.f"));
                }
                else
                {
                    return "Uptime: -";
                }
            }
        }

        public string TxSuccessfulTxt
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus == HidUtility.UsbConnectionStatus.Connected)
                    return string.Format("Sent: {0}", communicator.TxCount);
                else
                    return "Sent: -";
            }
        }

        public string TxFailedTxt
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus == HidUtility.UsbConnectionStatus.Connected)
                    return string.Format("Sending failed: {0}", communicator.TxFailedCount);
                else
                    return "Sending failed: -";
            }
        }

        public string RxSuccessfulTxt
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus == HidUtility.UsbConnectionStatus.Connected)
                    return string.Format("Received: {0}", communicator.RxCount);
                else
                    return "Receied: -";
            }
        }

        public string RxFailedTxt
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus == HidUtility.UsbConnectionStatus.Connected)
                    return string.Format("Reception failed: {0}", communicator.RxFailedCount);
                else
                    return "Reception failed: -";
            }
        }

        public string TxSpeedTxt
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus == HidUtility.UsbConnectionStatus.Connected)
                {
                    if (communicator.TxCount != 0)
                    {
                        return string.Format("TX Speed: {0:0.00} packets per second", communicator.TxCount / (DateTime.Now - ConnectedTimestamp).TotalSeconds);
                    }
                }
                return "TX Speed: n/a";
            }
        }

        public string RxSpeedTxt
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus == HidUtility.UsbConnectionStatus.Connected)
                {
                    if (communicator.TxCount != 0)
                    {
                        return string.Format("RX Speed: {0:0.00} packets per second", communicator.TxCount / (DateTime.Now - ConnectedTimestamp).TotalSeconds);
                    }
                }
                return "RX Speed: n/a";
            }
        }

        public double CurrentMeasurement
        {
            get
            {
                return -15.3;
                //return communicator.InputVoltage;
            }
        }

        public string CurrentMeasurementTxt
        {
            get
            {
                return string.Format("{0:0.0}dBm", communicator.CurrentMeasurement);
            }
        }

        public int CurrentMeasurementAdc
        {
            get
            {
                return communicator.CurrentMeasurementAdc;
            }
        }

        public string CurrentMeasurementAdcTxt
        {
            get
            {
                return communicator.CurrentMeasurementAdc.ToString();
            }
        }

        public string CurrentMeasurementVoltageTxt
        {
            get
            {
                return "0.123uV";
            }
        }

        public string CurrentMeasurementPowerTxt
        {
            get
            {
                return "8.74pW";
            }
        }

        public string CurrentMeasurementSTxt
        {
            get
            {
                return "S9+20";
            }
        }

        public string BarColor
        {
            get
            {
                return "MidnightBlue";
            }
        }

        public string ActivityLogVisibility
        {
            get
            {
                if (config.ActivityLogVisible)
                    return "Visible";
                else
                    return "Collapsed";
            }
            set
            {
                if (value == "Visible")
                    config.ActivityLogVisible = true;
                else
                    config.ActivityLogVisible = false;
            }
        }

        public string CommunicationVisibility
        {
            get
            {
                if (config.ConnectionDetailsVisible)
                    return "Visible";
                else
                    return "Collapsed";
            }
            set
            {
                if (value == "Visible")
                    config.ConnectionDetailsVisible = true;
                else
                    config.ConnectionDetailsVisible = false;
            }
        }

        public int WindowPositionX
        {
            get
            {
                return _WindowPositionX;
            }
            set
            {
                _WindowPositionX = value;
            }
        }

        public int WindowPositionY
        {
            get
            {
                return _WindowPositionY;
            }
            set
            {
                _WindowPositionY = value;
            }
        }

        public string Calibration00Txt
        {
            get { return _calibration[0].ToString(); }
            set { _calibration[0] = Int16.Parse(value); }
        }

        public string Calibration01Txt
        {
            get { return _calibration[1].ToString(); }
            set { _calibration[1] = Int16.Parse(value); }
        }

        public string Calibration02Txt
        {
            get { return _calibration[2].ToString(); }
            set { _calibration[2] = Int16.Parse(value); }
        }

        public string Calibration03Txt
        {
            get { return _calibration[3].ToString(); }
            set { _calibration[3] = Int16.Parse(value); }
        }

        public string Calibration04Txt
        {
            get { return _calibration[4].ToString(); }
            set { _calibration[4] = Int16.Parse(value); }
        }

        public string Calibration05Txt
        {
            get { return _calibration[5].ToString(); }
            set { _calibration[5] = Int16.Parse(value); }
        }

        public string Calibration06Txt
        {
            get { return _calibration[6].ToString(); }
            set { _calibration[6] = Int16.Parse(value); }
        }

        public string Calibration07Txt
        {
            get { return _calibration[7].ToString(); }
            set { _calibration[7] = Int16.Parse(value); }
        }

        public string Calibration08Txt
        {
            get { return _calibration[8].ToString(); }
            set { _calibration[8] = Int16.Parse(value); }
        }

        public string Calibration09Txt
        {
            get { return _calibration[9].ToString(); }
            set { _calibration[9] = Int16.Parse(value); }
        }

        public string Calibration10Txt
        {
            get { return _calibration[10].ToString(); }
            set { _calibration[10] = Int16.Parse(value); }
        }

        public string Calibration11Txt
        {
            get { return _calibration[11].ToString(); }
            set { _calibration[11] = Int16.Parse(value); }
        }

        public string Calibration12Txt
        {
            get { return _calibration[12].ToString(); }
            set { _calibration[12] = Int16.Parse(value); }
        }

        public string Calibration13Txt
        {
            get { return _calibration[13].ToString(); }
            set { _calibration[13] = Int16.Parse(value); }
        }
    }

}
