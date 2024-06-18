using AMESDanfossHMI.Properties;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Runtime;
using System.Text;
using System.Threading;
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
using static System.Net.Mime.MediaTypeNames;

namespace AMESDanfossHMI
{
    public partial class MainWindow : Window
    {

        private System.Net.Sockets.TcpClient tcpClient = new System.Net.Sockets.TcpClient();
        public System.IO.Ports.SerialPort BarcPort = new System.IO.Ports.SerialPort();
        private BackgroundWorker bgWorkerComms = new BackgroundWorker();
        private DispatcherTimer MainTimer = new DispatcherTimer();
        private Stopwatch swAlarms = new Stopwatch();
        public PLCIO PLC = new PLCIO();
        private Brush StockColor;   
        public bool bOneShot;
        public string sBarcBuffer;
        public string[] sBarcVal;
        Stopwatch swBarc = new Stopwatch();
        public int nAlarmIndex = 0;

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
                swBarc.Start();
                swAlarms.Start();
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

                tcpClient.Connect("10.1.119.15", 250);
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
                Environment.Exit(0);
            }
        }


        private void BarcPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            if (swBarc.ElapsedMilliseconds > 100) 
            {
                sBarcBuffer = "";
                swBarc.Restart();
            }
            BarcPort_ReceivedData(BarcPort.ReadExisting());
        }

        private void BarcPort_ReceivedData(string val)
        {        
                sBarcBuffer = sBarcBuffer + val;
        }
        private void comboboxBarcodePorts_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (BarcPort.IsOpen)
                {
                    BarcPort.Close();
                    BarcPort.Dispose();
                    BarcPort = new SerialPort();
                }
                BarcPort.BaudRate = 9600;
                BarcPort.DataBits = 8;
                BarcPort.StopBits = StopBits.One;
                BarcPort.Parity = Parity.None;
                BarcPort.PortName = comboboxBarcodePorts.SelectedItem.ToString();
                BarcPort.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void MainTimer_Tick(object sender, object e)
        {
            try
            {
                if (BarcPort.IsOpen & sBarcBuffer != "")
                {
                    txtboxBarcodeScan.Text = sBarcBuffer;
                }
                //txtboxBarcodeScan.Text = swAlarms.ElapsedMilliseconds.ToString();//swAlarms.ElapsedMilliseconds.ToString();


                if (PLC.Input1) 
                {
                    btnManualRotateR.Background = Brushes.Green;
                    btnManualRotateR.IsEnabled = false;
                }
                else 
                { 
                    btnManualRotateR.Background = StockColor;
                    btnManualRotateR.IsEnabled = true;
                }
                if (PLC.Input2)
                {
                    btnManualRotateR2.Background = Brushes.Green;
                    btnManualRotateR2.IsEnabled = false;
                }
                else
                {
                    btnManualRotateR2.Background = StockColor;
                    btnManualRotateR2.IsEnabled = true;
                }
                if (PLC.Input3) { btnTamper.IsEnabled = false; }
                else { btnTamper.IsEnabled= true; }
                if (PLC.Input4)
                {
                    btnRoboInfeed.IsEnabled = false;
                    btnRoboOutfeed.IsEnabled = false;
                    btnRoboIdle.IsEnabled = false;
                }
                else
                {
                    btnRoboInfeed.IsEnabled = true;
                    btnRoboOutfeed.IsEnabled = true;
                    btnRoboIdle.IsEnabled = true;
                }
                if (PLC.Output8) { btnPartRunOut.Background = Brushes.Green; }
                else { btnPartRunOut.Background = StockColor; }

                //txtboxBarcodeScan.Text = swAlarms.ElapsedMilliseconds.ToString() + ", " + i;
                if (swAlarms.ElapsedMilliseconds > 1800)
                {
                    swAlarms.Restart();
                    nAlarmIndex = 0;
                    lblStatus.Content = "";
                }

                if (!PLC.Input5 & nAlarmIndex == 0) //& swAlarms.ElapsedMilliseconds > 1500)
                {
                    //1
                    lblStatus.Content = "AutoStop";
                    nAlarmIndex = 1;
                    swAlarms.Restart();
                }
                if (!PLC.Input6 & nAlarmIndex < 2 & swAlarms.ElapsedMilliseconds > 1500)
                {
                    //2
                    lblStatus.Content = "E-Stop";
                    nAlarmIndex = 2;
                    swAlarms.Restart();
                }
                if (!PLC.Input7 & nAlarmIndex < 3 & swAlarms.ElapsedMilliseconds > 1500)
                {
                    //3
                    lblStatus.Content = "Check Air Pressure";
                    nAlarmIndex = 3;
                    swAlarms.Restart();
                }
                if (PLC.Input9 & nAlarmIndex < 4 & swAlarms.ElapsedMilliseconds > 1500)
                {
                    //4
                    lblStatus.Content = "Cylinder Error";
                    nAlarmIndex = 4;
                    swAlarms.Restart();
                }
                if (PLC.Input10 & nAlarmIndex < 5 & swAlarms.ElapsedMilliseconds > 1500)
                {
                    //5
                    lblStatus.Content = "Weber Label Error";
                    nAlarmIndex = 5;
                    swAlarms.Restart();
                }
                if (PLC.Input11 & nAlarmIndex < 6 & swAlarms.ElapsedMilliseconds > 1500)
                {
                    //6
                    lblStatus.Content = "Indexer Table Error";
                    nAlarmIndex = 6;
                    swAlarms.Restart();
                }
                if (PLC.Input12 & nAlarmIndex < 7 & swAlarms.ElapsedMilliseconds > 1500)
                {
                    //7
                    lblStatus.Content = "Rotary Table Error";
                    nAlarmIndex = 7;
                    swAlarms.Restart();
                }
                if (PLC.Input13 & nAlarmIndex < 8 & swAlarms.ElapsedMilliseconds > 1500)
                {
                    //8
                    lblStatus.Content = "Robot Error";
                    nAlarmIndex = 8;
                    swAlarms.Restart();
                }
                if (PLC.Input14 & nAlarmIndex < 9 & swAlarms.ElapsedMilliseconds > 1500)
                {
                    //9
                    lblStatus.Content = "Laser Error";
                    nAlarmIndex = 9;
                    swAlarms.Restart();
                }
                if (PLC.Input15 & nAlarmIndex < 10 & swAlarms.ElapsedMilliseconds > 1500)
                {
                    //10
                    LabelFumexFilterAlarm.Background = Brushes.Red;
                    lblStatus.Content = "Fumex Filter Change";
                    nAlarmIndex = 10;
                    swAlarms.Restart();
                }



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

            bool[] outputBool2 = { PLC.Output1, PLC.Output2, PLC.Output3, PLC.Output4, PLC.Output5, PLC.Output6, PLC.Output7, PLC.Output8 };
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

            bool[] outputBool3 = { PLC.Output9, PLC.Output10, PLC.Output11, PLC.Output12, PLC.Output13, PLC.Output14, PLC.Output15, PLC.Output16 };
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

            if (sBarcBuffer  != null)
            {
                result = null;
                result = BitConverter.GetBytes(sBarcBuffer.Length);
                combine = combine.Concat(result).ToArray();

                result = null;
                result = Encoding.ASCII.GetBytes(sBarcBuffer);
                combine = combine.Concat(result).ToArray();
            }


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


                PLC.Input1 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(0, 1)));
                PLC.Input2 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(1, 1)));
                PLC.Input3 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(2, 1)));
                PLC.Input4 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(3, 1)));
                PLC.Input5 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(4, 1)));//Fumex Running
                PLC.Input6 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(5, 1)));
                PLC.Input7 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(6, 1)));
                PLC.Input8 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(7, 1)));
                PLC.Input9 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(8, 1)));
                PLC.Input10 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(9, 1)));
                PLC.Input11 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(10, 1)));
                PLC.Input12 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(11, 1)));
                PLC.Input13 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(12, 1)));
                PLC.Input14 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(13, 1)));
                PLC.Input15 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(14, 1)));
                PLC.Input16 = Convert.ToBoolean(Convert.ToInt32(sBinary.Substring(15, 1)));
                if (PLC.Input1) { PLC.Output1 = false; } //Outfeed Table Moving Signal
                if (PLC.Input2) { PLC.Output2 = false; } //Fixture Table Moving Signal
                if (PLC.Input3) { PLC.Output3 = false; } //Tamper Running Signal
                if (PLC.Input4) 
                { 
                    PLC.Output4 = false;
                    PLC.Output5 = false;
                    PLC.Output7 = false;
                } //Robot Running Signal
                //AlarmArr[5] = !PLC.Input5;
                //AlarmArr[6] = !PLC.Input6;
                //AlarmArr[7] = !PLC.Input7;
                //AlarmArr[8] = PLC.Input8;
                //AlarmArr[9] = PLC.Input9;
                //AlarmArr[10] = PLC.Input10;
                //AlarmArr[11] = PLC.Input11;
                //AlarmArr[12] = PLC.Input12;
                //AlarmArr[13] = PLC.Input13;
                //AlarmArr[14] = PLC.Input14;
                //AlarmArr[15] = PLC.Input15;
                //nAlarmCount = CalculateValues(true);
            }

        }
        //int CalculateValues(bool val)
        //{
        //    //return AlarmArr.Count(c => c == val);
        //}
        public class PLCIO
        {
            public bool Input1 { get; set; }//Outfeed Table Moving Signal
            public bool Input2 { get; set; }//Fixture Table Moving Signal
            public bool Input3 { get; set; }//Tamper Running Signal
            public bool Input4 { get; set; }//Robot Running Signal
            public bool Input5 { get; set; }//AutoStop
            public bool Input6 { get; set; }//E-Stop
            public bool Input7 { get; set; }//Air Pressure Good
            public bool Input8 { get; set; }//Fumex Running
            public bool Input9 { get; set; }//Cylinder Error
            public bool Input10 { get; set; }//Tamper Error
            public bool Input11 { get; set; }//Index Table Error
            public bool Input12 { get; set; }//Outfeed Table Error
            public bool Input13 { get; set; }//Robot Error
            public bool Input14 { get; set; }//Laser Error
            public bool Input15 { get; set; }//Fumex Filter Change
            public bool Input16 { get; set; }
            public bool Output1 { get; set; }//Rotate Outfeed Table
            public bool Output2 { get; set; }//Rotate Fixture Table
            public bool Output3 { get; set; }//Run Tamper
            public bool Output4 { get; set; }//Trigger Robot Infeed 
            public bool Output5 { get; set; }//Trigger Robot Outfeed
            public bool Output6 { get; set; }//Fumex
            public bool Output7 { get; set; }//Robot IDLE Position
            public bool Output8 { get; set; }//Run Out
            public bool Output9 { get; set; }
            public bool Output10 { get; set; }
            public bool Output11 { get; set; }
            public bool Output12 { get; set; }
            public bool Output13 { get; set; }
            public bool Output14 { get; set; }
            public bool Output15 { get; set; }
            public bool Output16 { get; set; }
        }

        private void btnFumex_Click(object sender, RoutedEventArgs e)
        {
        
        }


        public bool IsNumeric(string value)
        {
            return value.All(char.IsNumber);
        }

        private void ButtonFumexFilter_Click(object sender, RoutedEventArgs e)
        {

        }
        
        private void btnManualRotateR_Click(object sender, RoutedEventArgs e)
        {
            if (!PLC.Output1) { PLC.Output1 = true; }
        }

        private void btnManualRotateR2_Click(object sender, RoutedEventArgs e)
        {
            if (!PLC.Output2) { PLC.Output2 = true; }
        }

        private void btnClose_Click_1(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void btnTamper_Click(object sender, RoutedEventArgs e)
        {
            if (!PLC.Output3) { PLC.Output3 = true; }
        }

        private void btnRoboInfeed_Click(object sender, RoutedEventArgs e)
        {
            if (!PLC.Output4) { PLC.Output4 = true; }
        }

        private void btnRoboOutfeed_Click(object sender, RoutedEventArgs e)
        {
            if (!PLC.Output5) { PLC.Output5 = true; }
        }

        private void btnFumex_Click_1(object sender, RoutedEventArgs e)
        {
            if (PLC.Output6) { PLC.Output6 = false; }
            else { PLC.Output6 = true; }
        }

        private void btnRoboIdle_Click(object sender, RoutedEventArgs e)
        {
            if (!PLC.Output7) {  PLC.Output7 = true; }
        }

        private void btnPartRunOut_Click_1(object sender, RoutedEventArgs e)
        {
            if (!PLC.Output8) { PLC.Output8=true; }
            else { PLC.Output8=false; }
        }

        private void ToggleOperatorMode_Checked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Op Mode");
        }

        private void ToggleOperatorMode_Unchecked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("NonOp Mode");
        }

        private void ButtonFumexFilter_Click_1(object sender, RoutedEventArgs e)
        {
            LabelFumexFilterAlarm.Background = Brushes.Green;
        }
    }
}
