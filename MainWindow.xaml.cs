using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AMESDanfossHMI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private System.Net.Sockets.TcpClient tcpClient = new System.Net.Sockets.TcpClient();
        public System.IO.Ports.SerialPort BarcPort = new System.IO.Ports.SerialPort();
        private BackgroundWorker bgWorkerComms = new BackgroundWorker();
        private DispatcherTimer MainTimer = new DispatcherTimer();
        public PLCCLass PLC = new PLCCLass();
        private Brush StockColor;   
        public bool bOneShot;
        public string sBarcBuffer;
        public MainWindow()
        {
            InitializeComponent();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                StockColor = btnFumex.Background;
                MainTimer.Tick += MainTimer_Tick;
                MainTimer.Interval = TimeSpan.FromMilliseconds(2);
                MainTimer.Start();    
                BarcPort.DataReceived += BarcPort_DataReceived;
                //lblPartFailCount.DataContext = setti;
                //lblChuteCount.DataContext = setti;
                //lblReqChuteCount.DataContext = setti;
                //lblBarc1Grade.DataContext = setti;
                //lblBarc2Grade.DataContext = setti;
                string[] ports = SerialPort.GetPortNames();
                if (ports.Length > 0)
                {
                    foreach (var item in ports)
                    {
                        comboboxBarcodePorts.Items.Add(item);
                    }
                }

                tcpClient.Connect("192.168.1.5", 250);
                if (tcpClient.Connected)
                {
                    bgWorkerComms.DoWork += BgWorkerComms_DoWork;
                    bgWorkerComms.RunWorkerAsync();
                }
                else
                {
                    MessageBox.Show("PLC Connection Failed");
                    Environment.Exit(0);
                }
                //BarcodePort.BaudRate = 115200
                //    BarcodePort.DataBits = 8
                //    BarcodePort.StopBits = StopBits.One
                //    BarcodePort.Parity = Parity.Even
                //    BarcodePort.PortName = "COM" & Settings.Aplication._BarcodePort
                //    'MessageBox.Show(SerialPortBarc1.PortName)
                //    BarcodePort.Open()

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BarcPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            BarcPort_ReceivedData(BarcPort.ReadExisting());
        }

        private void BarcPort_ReceivedData(string val)
        {
            sBarcBuffer += val;
            if (IsNumeric(sBarcBuffer.Trim()))
            {
                PLC.SetRemainingPartsVal = Convert.ToInt32(sBarcBuffer.Trim());
                PLC.SetRemainingParts = true;
                sBarcBuffer = "";
            }

        }

   
        private void MainTimer_Tick(object sender, object e)
        {
            try
            {
                if (PLC.iStart) { btnManualRotate.Background = new SolidColorBrush(Colors.Green); }
                else { btnManualRotate.Background = StockColor; }

                //if (PLC.oManualRotate) { btnManualRotate.Background = new SolidColorBrush(Colors.Green); }
                //else { btnManualRotate.Background = StockColor; }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BgWorkerComms_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                do
                {
                    omComm(0);
                } while (true);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private byte[] omMessage(byte[] nBytes)
        {
            try
            {
                NetworkStream NetworkStream = tcpClient.GetStream();
                if (NetworkStream.CanWrite & NetworkStream.CanRead)
                {
                    NetworkStream.Write(nBytes, 0, nBytes.Length);
                    byte[] bytes = new byte[480];
                    NetworkStream.Read(bytes, 0, 256);
                    return bytes;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void omComm(byte nDriveNum)
        {
            //If Zaxis.niCurrentPos > Zaxis.noDdPosition - nPosWindow * Settings.Aplication._nZScaleFactor And Zaxis.niCurrentPos < Zaxis.noDdPosition + nPosWindow * Settings.Aplication._nZScaleFactor Then Zaxis.oTrigger = False

            //SEND============================================================================================================================================
            byte[] combine = BitConverter.GetBytes(0);
            byte[] result = BitConverter.GetBytes(0);

            combine = combine.Concat(result).ToArray();

            //Array.Resize(ref combine, combine.Length + 1);
            //combine[combine.Length - 1] = nDriveNum;

            bool[] outputBool2 = { PLC.oManualRotate, PLC.oPartRunOut, PLC.oFumex, PLC.oCheckClockAck, PLC.oAckPartCount, PLC.oChuteCountTotReset, PLC.PLCConnectHeartbeat, PLC.RedRabbitErrorACK };
            byte outputByte2 = 0;
            int index2 = 0;
            foreach (bool b2 in outputBool2)
            {
                if (b2)
                    outputByte2 |= (byte)(1 << (index2));
                index2 += 1;
            }

            Array.Resize(ref combine, combine.Length + 1);
            combine[combine.Length - 1] = outputByte2;

            bool[] outputBool3 = { PLC.RunRedRabbitNotifyACK, PLC.RedRabbitUIOpen, PLC.SetRemainingParts, false, false, false, false, false };
            byte outputByte3 = 0;
            int index3 = 0;
            foreach (bool b3 in outputBool3)
            {
                if (b3)
                    outputByte3 |= (byte)(1 << (index3));
                index3 += 1;
            }

            Array.Resize(ref combine, combine.Length + 1);
            combine[combine.Length - 1] = outputByte3;



            //RECIEVE==========================================================================================================================================
            byte[] omReturn = omMessage(combine);

            if (omReturn != null)
            {
                long TestBits = BitConverter.ToInt32(omReturn, 0);
                //Int32 iBits = BitConverter.ToInt16(omReturn, 1);

                string sBinary = Convert.ToString(TestBits, 2).PadLeft(32, '0');
                sBinary = new string(sBinary.Reverse().ToArray());

                //    OmAxis.iMoving = StringToBool(sBinary(0))
                //OmAxis.iReady = StringToBool(sBinary(1))
                //OmAxis.iAlarm = StringToBool(sBinary(2))
                //OmAxis.iVA = StringToBool(sBinary(3))


                PLC.iStart = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(0, 1)));
                PLC.iEStop = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(9, 1)));
                PLC.iAutoStop = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(10, 1)));
                PLC.iRotateAck = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(11, 1)));
                PLC.Sprocket1 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(12, 1)));
                PLC.Sprocket2 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(13, 1)));
                PLC.SequenceRunning = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(14, 1)));
                PLC.RotaryMoving = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(15, 1)));
                PLC.iCheckClock = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(16, 1)));
                PLC.iIndexPartCount = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(17, 1)));
                PLC.iNotifyPartCount = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(18, 1)));
                PLC.isMarking = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(19, 1)));
                PLC.iFumexFilter = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(20, 1)));
                PLC.iFumexRunning = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(21, 1)));
                PLC.RedRabbitRan = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(22, 1)));
                PLC.Station1 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(23, 1)));
                PLC.Station2 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(24, 1)));
                PLC.Station3 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(25, 1)));
                PLC.Station4 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(26, 1)));
                PLC.Station5 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(27, 1)));
                PLC.Station6 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(28, 1)));
                PLC.RedRabbitNextStation = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(29, 1)));
                PLC.RedRabbitError = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(30, 1)));
                PLC.RunRedRabbitNotify = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(31, 1))); 
            }

        }

        public class PLCCLass
        {
            public bool iStart { get; set; }
            public bool iEStop { get; set; }
            public bool iAutoStop { get; set; }
            public bool iRotateAck { get; set; }
            public bool oFumex { get; set; }
            public bool oPartRunOut { get; set; }
            public bool oManualRotate { get; set; }
            public bool Sprocket1 { get; set; }
            public bool Sprocket2 { get; set; }
            public bool SequenceRunning { get; set; }
            public bool RotaryMoving { get; set; }
            public bool iCheckClock { get; set; }
            public bool oCheckClockAck { get; set; }
            public bool iIndexPartCount { get; set; }
            public bool iNotifyPartCount { get; set; }
            public bool oAckPartCount { get; set; }
            public bool isMarking { get; set; }
            public int nRequiredChuteCount { get; set; }
            public int nChuteCount { get; set; }
            public Int16 nBarc1Grade { get; set; }
            public Int16 nBarc2Grade { get; set; }
            public int nChuteCountTotal { get; set; }
            public bool oChuteCountTotReset { get; set; }
            public bool iFumexFilter { get; set; }
            public bool iFumexRunning { get; set; }
            public bool Station1 { get; set; }
            public bool Station2 { get; set; }
            public bool Station3 { get; set; }
            public bool Station4 { get; set; }
            public bool Station5 { get; set; }
            public bool Station6 { get; set; }
            public bool RedRabbitRan { get; set; }
            public bool RedRabbitNextStation { get; set; }
            public bool RedRabbitError { get; set; }
            public bool RedRabbitErrorACK { get; set; }
            public bool PLCConnectHeartbeat { get; set; }
            public bool RunRedRabbitNotify { get; set; }
            public bool RunRedRabbitNotifyACK { get; set; }
            public bool InnerKnurlLzrReady { get; set; }
            public bool OuterKnurlLzrReady { get; set; }
            public bool BarcLzrReady { get; set; }
            public bool StartPB { get; set; }
            public bool RedRabbitUIOpen { get; set; }
            public bool SetRemainingParts { get; set; }

            public int SetRemainingPartsVal { get; set; }
            public bool PLCStartQueued { get; set; }
        }

        private void btnFumex_Click(object sender, RoutedEventArgs e)
        {
            if (PLC.oFumex)
            {
                PLC.oFumex = false;
            }
            else
            {
                PLC.oFumex = true;
            }
        }

        private void btnPartRunOut_Click(object sender, RoutedEventArgs e)
        {
            if (PLC.oPartRunOut)
            {
                PLC.oPartRunOut = false;
            }
            else
            {
                PLC.oPartRunOut = true;
            }
        }

        private void btnManualRotate_Click(object sender, RoutedEventArgs e)
        {
            if (PLC.oManualRotate)
            {
                PLC.oManualRotate = false;
                //btnManualRotate.Background = StockColor;
            }
            else
            {
                PLC.oManualRotate = true;
                //btnManualRotate.Background = new SolidColorBrush(Colors.Green);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        public bool IsNumeric(string value)
        {
            return value.All(char.IsNumber);
        }
        private void btnResetPart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //if (Keyboard.IsKeyDown(Key.LeftCtrl))
                //{
                //    string sInput = Microsoft.VisualBasic.Interaction.InputBox("Set Counter Value:", "Manual Counter Set");
                //    if(sInput != "")
                //    {
                //        if (IsNumeric(sInput))
                //        {
                //            setti.PartFailCount = Convert.ToInt32(sInput);
                //        }
                //        else
                //        {
                //            MessageBox.Show("Value Non Numeric");
                //        }
                //    }
                //}
                //else
                //{
                //    setti.PartFailCount = 0;
                //}
                PLC.oChuteCountTotReset = true;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void ButtonFumexFilter_Click(object sender, RoutedEventArgs e)
        {

        }

        private void comboboxBarcodePorts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (BarcPort.IsOpen)
                {
                    BarcPort.Close();
                    BarcPort.Dispose();
                    BarcPort = new SerialPort();
                }
                BarcPort.BaudRate = 115200;
                BarcPort.DataBits = 8;
                BarcPort.StopBits = StopBits.One;
                BarcPort.Parity = Parity.Even;
                BarcPort.PortName = comboboxBarcodePorts.SelectedItem.ToString();
                BarcPort.Open();
            }
            catch (Exception)
            {

                throw;
            }
        }

     

        private void comboboxFixtureSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btnManualRotate_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PLC.oManualRotate) { PLC.oManualRotate = false; }
                else { PLC.oManualRotate = true;}
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
