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
        public PLCIO PLC = new PLCIO();
        private Brush StockColor;   
        public bool bOneShot;
        public string sBarcBuffer;
        public string[] sBarcVal;
        int bytestoread;
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

        private void BarcPort_PinChanged(object sender, SerialPinChangedEventArgs e)
        {
            MessageBox.Show("Pin Changed");
        }
        int barcIndex;
        private void BarcPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            //sBarcBuffer = "";
            
            BarcPort_ReceivedData(BarcPort.ReadExisting());
        }

        private void BarcPort_ReceivedData(string val)
        {
            
                
                sBarcBuffer = sBarcBuffer + val;
           
            
            //if (sBarcBuffer.EndsWith("\r") || sBarcBuffer.EndsWith("\n"))
            //{
                //sBarcBuffer = sBarcBuffer.Substring(0, sBarcBuffer.Length - 1);
                //if (sBarcVal != null) { Array.Clear(sBarcVal, 0, sBarcVal.Length); }
                //sBarcVal = Strings.Split(sBarcBuffer, ",");
                //sBarcBuffer = "";
                //sBarcVal = Barc1Data;
                //foreach (string item in Barc1DataArray)
                //{
                //    if (item == "ERROR") { MessageBox.Show("Data Error"); break; }
                //}
                //if (Settings.BarcSequenceSelect)
                //{
                //    if (Settings.BarcDatabase)
                //    {
                //        DataRow sBarcCheck = BarcDB.Select("Barcode = '" + BarcodeValue + "'").FirstOrDefault();
                //        if (sBarcCheck != null)
                //        {
                //            Application.Current.Dispatcher.BeginInvoke(new Action(() => dt.Clear()));
                //            string barcpathseq = sSeqPath + sBarcCheck.ItemArray[1] + ".XML";
                //            if (System.IO.File.Exists(barcpathseq))
                //            {
                //                Application.Current.Dispatcher.BeginInvoke(new Action(() => dt.ReadXml(barcpathseq)));
                //            }
                //            else { MessageBox.Show("Sequence Not Found"); }
                //            Settings.aplication._sCurrentSeq = sSeqPath + sBarcCheck.ItemArray[1] + ".XML";
                //            CurrentSeq = "Current Seq: " + "\"" + Settings.aplication._sCurrentSeq + "\"";

                //        }
                //        else
                //        {
                //            MessageBox.Show("Sequence Not Found in Database");
                //        }
                //    }
                //    else
                //    {
                //        foreach (string sequence in Directory.GetFiles(sSeqPath))
                //        {
                //            if (sequence.Contains(BarcodeValue))
                //            {
                //                Application.Current.Dispatcher.BeginInvoke(new Action(() => dt.Clear()));
                //                Application.Current.Dispatcher.BeginInvoke(new Action(() => dt.ReadXml(sequence)));
                //                Settings.aplication._sCurrentSeq = sequence;
                //                CurrentSeq = "Current Seq: " + "\"" + Settings.aplication._sCurrentSeq + "\"";
                //                break;
                //            }
                //        }
                //    }
                //}
            //}
            //bBarcodeScanned = true;
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
            catch (Exception)
            {

                throw;
            }
        }
        private void MainTimer_Tick(object sender, object e)
        {
            try
            {
                if(sBarcBuffer != "")
                {
                   // txtboxBarcodeScan.Text = sBarcBuffer;
                }
                if (BarcPort.IsOpen & sBarcBuffer != "")
                {
                    txtboxBarcodeScan.Text = sBarcBuffer;
                }
                
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
            }

        }

        public class PLCIO
        {
            public bool Input1 { get; set; }//Outfeed Table Moving Signal
            public bool Input2 { get; set; }//Fixture Table Moving Signal
            public bool Input3 { get; set; }//Tamper Running Signal
            public bool Input4 { get; set; }//Robot Running Signal
            public bool Input5 { get; set; }
            public bool Input6 { get; set; }
            public bool Input7 { get; set; }
            public bool Input8 { get; set; }
            public bool Input9 { get; set; }
            public bool Input10 { get; set; }
            public bool Input11 { get; set; }
            public bool Input12 { get; set; }
            public bool Input13 { get; set; }
            public bool Input14 { get; set; }
            public bool Input15 { get; set; }
            public bool Input16 { get; set; }
            public bool Output1 { get; set; }//Rotate Outfeed Table
            public bool Output2 { get; set; }//Rotate Fixture Table
            public bool Output3 { get; set; }//Run Tamper
            public bool Output4 { get; set; }//Trigger Robot Infeed 
            public bool Output5 { get; set; }//Trigger Robot Outfeed
            public bool Output6 { get; set; }//Fumex
            public bool Output7 { get; set; }//Robot IDLE Position
            public bool Output8 { get; set; }
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

        private void btnPartRunOut_Click(object sender, RoutedEventArgs e)
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


    }
}
