using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;


namespace InstruLab
{
    class Comms_thread
    {
        //promenne pro pripojeni a spravu devices
        private bool find_request = false;
        private bool connected = false;
        private int searchingState = 0;
        public int progress = 0;

        private bool port_open_req = false;
       

        private int error = 0;

        private int numberOfPorts = 0;
        private List<Device> devices = new List<Device>();
        private Device connectedDevice;
        private bool newDevices = false;
        private bool run_th = true;
        SerialPort serialPort;


        public void run()
        {
            while (run_th)
            {
                if (find_request)
                {
                    find_devices();
                }
                if (port_open_req) {
                    port_open_req = false;
                    connectedDevice.open_port();
                }

            }
        }

        public void stop()
        {
            this.run_th = false;
        }

        public void find_devices_request() {
            find_request = true;
            searchingState = 1;
        }


        // nalezne vsechna pripojena zarizeni a da je do listu
        public void find_devices()
        {
            devices.Clear();

            numberOfPorts = 0;

            if (!connected)
            {
                serialPort = new SerialPort();
                serialPort.ReadBufferSize = 128 * 1024;
                serialPort.BaudRate = 115200;
                this.error = 0;

                foreach (string s in SerialPort.GetPortNames())
                {
                    numberOfPorts++;
                }

                int counter = 0;
                foreach (string serial in SerialPort.GetPortNames())
                {
                    counter++;
                    progress = (counter * 100) / numberOfPorts;
                    try
                    {
                        Thread.Yield();
                        serialPort.PortName = serial;
                        serialPort.Open();
                        serialPort.Write(Commands.IDNRequest+";");
                        Thread.Sleep(250);

                        char[] msg = new char[256];
                        int toRead = serialPort.BytesToRead;

                        serialPort.Read(msg, 0, toRead);
                        string msgInput = new string(msg, 0, 4);
                        string deviceName = new string(msg, 4, toRead - 4);

                        Thread.Yield();
                        if (msgInput.Equals(Commands.ACKNOWLEDGE))
                        {
                            devices.Add(new Device(serialPort.PortName, deviceName, serialPort.BaudRate));
                        }
                        serialPort.Close();
                        serialPort.Dispose();
                    }
                    catch (Exception ex)
                    {
                        if (serialPort.IsOpen)
                        {
                            serialPort.Close();
                        }
                        Console.WriteLine(ex);
                    }
                }
                newDevices = true;
                find_request = false;
                this.searchingState = 2;
            }
        }

        public int get_searching_state() {
            if (searchingState == 2)
            {
                this.searchingState = 0;
                return 2;
            }
            else
            {
                return this.searchingState;
            }
        }

        public int get_progress(){
            return this.progress;
        }
        public int get_num_of_devices()
        {
            return this.devices.Count;
        }

        public bool is_new_devices()
        {
            return this.newDevices;
        }

        public string[] get_dev_names()
        {
            string[] result = new string[devices.Count()];
            newDevices = false;
            int i = 0;
            foreach (Device d in devices)
            {
                result[i] = d.get_port()+":"+d.get_name();
                i++;
            }
            return result;
        }

        public void connect_device(string port)
        {
            foreach (Device d in devices)
            {
                if (port.Equals(d.get_port()))
                {
                    this.connectedDevice = d;
                    port_open_req = true;
                    break;
                }
            }
        }


    }
}
