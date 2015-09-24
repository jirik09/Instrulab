using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Windows.Forms;
using System.Threading;

namespace InstruLab
{
    class Device
    {
        private SerialPort port;
        private string portName;
        private string name;
        private string mcu;
        private int speed;
        private bool opened = false;
        private bool configured = false;


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

        public void open_port()
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
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fatal error during connecting to device \r\n" + ex, "Fatal error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        public void load_config() {
            if (port.IsOpen) {
                port.Write(Commands.SYSTEM+":"+Commands.CONFIGRequest + ";");
                Thread.Sleep(250);
                char[] msg = new char[256];
                int toRead = port.BytesToRead;
                port.Read(msg, 0, toRead);
                string msgInput = new string(msg, 0, toRead);
                Console.WriteLine(msgInput);
                //TODO donacist config + update Gui behem cteni configu

            }
        }
    }
}
