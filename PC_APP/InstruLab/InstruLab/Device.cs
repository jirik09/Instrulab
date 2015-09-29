using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace InstruLab
{
    public class Device
    {
        public struct SystemConfig_def
        {
            public int CoreClock;
            public int PeriphClock;
            public string MCU;
        }

        public struct CommsConfig_def
        {
            public int bufferLength;
            public int UartSpeed;
            public string TX_pin;
            public string RX_pin;
            public bool useUsb;
            public string DP_pin;
            public string DM_pin;
        }

        public struct ScopeConfig_def
        {
            public int maxSamplingFrequency;
            public int maxBufferLength;
            public int maxNumChannels;
            public string[] pins;
            public int VRef;
            public int munRanges;
            public int[,] ranges;
            public byte[] buffer;
            public UInt16[,] samples;
            public double[] timeBase;
            public Scope.mode_def mode;
            public double maxTime;
            public int sampligFreq;
            public int actualChannels;
            public int actualRes;
        }

        public struct GeneratorConfig_def
        {
            public int samplingFrequency;
            public int BufferLength;
            public int dataDepth;
            public int numChannels;
            public string[] pins;
            public int VRef;
        }

        private SerialPort port;
        private string portName;
        private string name;
        private string mcu;
        private int speed;
        private bool opened = false;
        private bool configured = false;
        private SystemConfig_def systemCfg;
        public CommsConfig_def commsCfg;
        public ScopeConfig_def scopeCfg;
        public GeneratorConfig_def genCfg;

        Scope Scope_form;


        public Device(string portName, string name, int speed)
        {
            this.portName = portName;
            this.name = name;
            this.speed = speed;
        }

        public string get_processor()
        {
            return this.mcu;
        }

        public string get_name()
        {
            return this.name;
        }

        public string get_port()
        {
            return this.portName;
        }

        public bool open_port()
        {
            try
            {
                this.port = new SerialPort();
                this.port.PortName = portName;
                this.port.ReadBufferSize = 512*1024;
                this.port.BaudRate = speed;
                port.Open();
                opened = true;
                configured = false;
                load_config();
                configured = true;
                port.Close();
                port.ReadTimeout = 2000;
                port.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.serialPort_DataReceived);
                port.ErrorReceived += new System.IO.Ports.SerialErrorReceivedEventHandler(this.serialPort_ErrorReceived);
                port.Open();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fatal error during connecting to device \r\n" + ex, "Fatal error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

        }

        public void load_config()
        {
            if (port.IsOpen)
            {
                port.Write(Commands.SYSTEM + ":" + Commands.CONFIGRequest + ";");
                char[] msg_char = new char[256];
                byte[] msg_byte = new byte[256];
                Thread.Sleep(250);                
                int toRead = port.BytesToRead;
                port.Read(msg_byte, 0, toRead);
                msg_char = System.Text.Encoding.ASCII.GetString(msg_byte).ToCharArray();

                if (new string(msg_char, 0, 4).Equals("SYST"))
                {
                    this.systemCfg.CoreClock = BitConverter.ToInt32(msg_byte, 4);
                    this.systemCfg.PeriphClock = BitConverter.ToInt32(msg_byte, 8);
                    this.systemCfg.MCU = new string(msg_char, 12, toRead-16);
                }


                port.Write(Commands.COMMS + ":" + Commands.CONFIGRequest + ";");
                Thread.Sleep(250);
                toRead = port.BytesToRead;
                port.Read(msg_byte, 0, toRead);
                msg_char = System.Text.Encoding.ASCII.GetString(msg_byte).ToCharArray();

                if (new string(msg_char, 0, 4).Equals("COMM"))
                {
                    this.commsCfg.bufferLength = BitConverter.ToInt32(msg_byte, 4);
                    this.commsCfg.UartSpeed = BitConverter.ToInt32(msg_byte, 8);
                    this.commsCfg.TX_pin = new string(msg_char, 12,4);
                    this.commsCfg.RX_pin = new string(msg_char, 16,4);
                    if (toRead > 20)
                    {
                        this.commsCfg.DP_pin = new string(msg_char, 24, 4);
                        this.commsCfg.DM_pin = new string(msg_char, 28, 4);
                        this.commsCfg.useUsb = true;
                    }else{
                        this.commsCfg.useUsb = false;
                    }
                }

                port.Write(Commands.SCOPE + ":" + Commands.CONFIGRequest + ";");
                Thread.Sleep(250);
                toRead = port.BytesToRead;
                port.Read(msg_byte, 0, toRead);
                msg_char = System.Text.Encoding.ASCII.GetString(msg_byte).ToCharArray();

                if (new string(msg_char, 0, 4).Equals("OSCP"))
                {

                    scopeCfg.maxSamplingFrequency = BitConverter.ToInt32(msg_byte, 4);
                    scopeCfg.maxBufferLength = BitConverter.ToInt32(msg_byte, 8);
                    scopeCfg.maxNumChannels = BitConverter.ToInt32(msg_byte, 12);
                    scopeCfg.pins=new string[scopeCfg.maxNumChannels];
                    for (int i = 0; i < this.scopeCfg.maxNumChannels; i++) {
                        scopeCfg.pins[i] = new string(msg_char, 16+4*i, 4);
                    }
                    scopeCfg.VRef = BitConverter.ToInt32(msg_byte, 16 + 4 * scopeCfg.maxNumChannels);

                    scopeCfg.munRanges = (toRead - 24 - 4 * scopeCfg.maxNumChannels) / 4;
                    scopeCfg.ranges= new int[2,scopeCfg.munRanges];
                    for (int i = 0; i < this.scopeCfg.munRanges; i++)
                    {
                        scopeCfg.ranges[0, i] = BitConverter.ToInt16(msg_byte, 20 + 4 * scopeCfg.maxNumChannels + 4 * i);
                        scopeCfg.ranges[1, i] = BitConverter.ToInt16(msg_byte, 22 + 4 * scopeCfg.maxNumChannels + 4 * i);
                    }
                    

                }

                port.Write(Commands.GENERATOR + ":" + Commands.CONFIGRequest + ";");
                Thread.Sleep(250);
                toRead = port.BytesToRead;
                port.Read(msg_byte, 0, toRead);
                msg_char = System.Text.Encoding.ASCII.GetString(msg_byte).ToCharArray();

                if (new string(msg_char, 0, 4).Equals("GEN_"))
                {
                    genCfg.samplingFrequency = BitConverter.ToInt32(msg_byte, 4);
                    genCfg.BufferLength = BitConverter.ToInt32(msg_byte, 8);
                    genCfg.dataDepth = BitConverter.ToInt32(msg_byte, 12);
                    genCfg.numChannels = BitConverter.ToInt32(msg_byte, 16);
                    genCfg.pins = new string[genCfg.numChannels];
                    for (int i = 0; i < this.genCfg.numChannels; i++)
                    {
                        genCfg.pins[i] = new string(msg_char, 20 + 4 * i, 4);
                    }
                    genCfg.VRef = BitConverter.ToInt32(msg_byte, 20 + 4 * genCfg.numChannels);
                }
            }
        }

        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            char[] inputMsg = new char[4];
            byte[] inputData = new byte[4];
            
            while (port.IsOpen && port.BytesToRead > 0)
            {
              /* inputMsg[0] = inputMsg[1]; //safe for un aligned messages
                inputMsg[1] = inputMsg[2];
                inputMsg[2] = inputMsg[3];
                inputMsg[3] = (char)port.ReadChar();*/
                while (port.BytesToRead < 4) { Thread.Yield(); }
                port.Read(inputMsg, 0, 4);
                switch (new string(inputMsg, 0, 4)) {
                    case Commands.SCOPE_INCOME:
                        int res;
                        int leng;
                        int currChan;
                        int numChan;
                        int i = 0;
                        int partsLen = 4096;
                        while (port.BytesToRead < 12)
                        {
                            Thread.Yield();
                        }
                        port.Read(inputData, 0, 4);
                        res = port.ReadByte();
                        leng = port.ReadByte() * 65536 + port.ReadByte() * 256 + port.ReadByte();
                        port.Read(inputData, 0, 2);
                        currChan = port.ReadByte();
                        numChan = port.ReadByte();
                        scopeCfg.buffer = new Byte[leng];

                        if (leng > partsLen)
                        {
                            int toRead = leng;
                            i = 0;
                            while (toRead > partsLen)
                            {
                                while (port.BytesToRead < partsLen)
                                {
                                    Thread.Yield();
                                }
                                port.Read(scopeCfg.buffer, i * partsLen, partsLen);
                                i++;
                                toRead -= partsLen;
                            }
                            while (port.BytesToRead < toRead)
                            {
                                Thread.Yield();
                            }
                            port.Read(scopeCfg.buffer, i * partsLen, toRead);
                        }
                        else
                        {
                            while (port.BytesToRead < leng)
                            {
                                Thread.Yield();
                            }
                            port.Read(scopeCfg.buffer, 0, leng);
                        }

                        
                        


                        if (res > 8) //resolution >8 bits
                        {
                            if (currChan == 1)
                            {
                                scopeCfg.samples = new UInt16[numChan, leng / 2];
                            }
                            for (i = 0; i < leng / 2; i++)
                            {
                                scopeCfg.samples[currChan-1, i] = BitConverter.ToUInt16(scopeCfg.buffer, i * 2);
                            }
                        }
                        else  //resolution <=8 bits
                        {
                            if (currChan == 1)
                            {
                                scopeCfg.samples = new UInt16[numChan, leng];
                            }
                            for (i = 0; i < leng; i++)
                            {
                                scopeCfg.samples[currChan-1, i] = scopeCfg.buffer[i];
                            }
                        }


                        if (currChan == numChan) {
                            if (res > 8)
                            {
                                scopeCfg.timeBase = new double[leng / 2];
                                generate_time_base(scopeCfg.sampligFreq, leng/2);
                            }
                            else
                            {
                                scopeCfg.timeBase = new double[leng];
                                generate_time_base(scopeCfg.sampligFreq, leng);
                            }
                            scopeCfg.actualChannels = numChan;
                            scopeCfg.actualRes = res;
                            Scope_form.add_message(new Message(Message.MsgRequest.SCOPE_NEW_DATA));
                            if (scopeCfg.mode == Scope.mode_def.AUTO || scopeCfg.mode == Scope.mode_def.NORMAL) {
                                Thread.Sleep(100);
                                send(Commands.SCOPE + ":" + Commands.SCOPE_NEXT + ";");
                            }
                        }
                        //Console.WriteLine("SCOPE DATA RECIEVED: Leng "+leng+", Res "+res+", Chan "+currChan+" of "+numChan);
                        break;

                    case Commands.ACKNOWLEDGE:
                        //Console.WriteLine(Commands.ACKNOWLEDGE);
                        break;
                    case Commands.TRIGGERED:
                        //Console.WriteLine(Commands.TRIGGERED);
                        Scope_form.add_message(new Message(Message.MsgRequest.SCOPE_TRIGGERED));
                        break;
                    default:
                        if (inputMsg[0] == Commands.ERROR) {
                            MessageBox.Show("Error recieved \r\n" + new string(inputMsg, 0, 4), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            scopeCfg.mode = Scope.mode_def.IDLE;
                        }
                        //Console.WriteLine(new string(inputMsg, 0, 4));
                        break;
                    
                }

                Thread.Yield();
            }
        }

        private void serialPort_ErrorReceived(object sender, System.IO.Ports.SerialErrorReceivedEventArgs e)
        {
            
        }



        private void generate_time_base(int sampling, int lenght)
        {
            for (int i = 0; i < lenght; i++)
            {
                scopeCfg.timeBase[i] = (double)(i) / sampling;
            }
            scopeCfg.maxTime = (double)(lenght) / sampling;
        }

        public void open_scope() {
            if (Scope_form == null || Scope_form.IsDisposed)
            {
                Scope_form = new Scope(this);
                Scope_form.Show();
            }
            else
            {
                Scope_form.BringToFront();
            }
        }

        public void close_scope() {
            if (Scope_form != null)
            {
                Scope_form.Close();
            }
        }
        
        public SystemConfig_def getSystemCfg() {
            return this.systemCfg;
        }
        public CommsConfig_def getCommsCfg()
        {
            return this.commsCfg;
        }


        public void set_scope_mode(Scope.mode_def mod) {
            this.scopeCfg.mode = mod;
        }

        public Scope.mode_def get_scope_mode() {
            return scopeCfg.mode;
        }

        




        public void send(string s)
        {
            try
            {
                port.Write(s);

                //   if (!s.Equals("OSCP:SRAT")) {
                //logUart(s);
                // }
              //  Console.WriteLine(s);
            }
            catch (Exception ex)
            {
                //Log("Data se nepodařilo odeslat:\r\n" + ex);
                Console.WriteLine(ex);
            }
        }

        public void send_short(int l)
        {
            byte[] bt = BitConverter.GetBytes(l);
            byte[] se = new byte[4];
            se[0] = 0;
            se[1] = 0;
            se[2] = bt[0];
            se[3] = bt[1];
            //logText(l.ToString() + "(0x" + BitConverter.ToString(se, 0).Replace("-", "") + ")");
            port.Write(bt, 0, 4);
           // Console.WriteLine(l.ToString());
        }

        public void send_int(int l)
        {
            byte[] bt = BitConverter.GetBytes(l);
            byte[] se = new byte[4];

            se[0] = bt[0];
            se[1] = bt[1];
            se[2] = bt[2];
            se[3] = bt[3];
            //logText(l.ToString() + "(0x" + BitConverter.ToString(se, 0).Replace("-", "") + ")");
            port.Write(se, 0, 4);
           // Console.WriteLine(l.ToString());
        }


        //end class
    }
}
