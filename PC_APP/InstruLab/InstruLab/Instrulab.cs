using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ZedGraph;
using System.Threading;
using System.Timers;

namespace InstruLab
{
    public partial class Instrulab : Form
    {
        Thread comm_th;
        Comms_thread comms;

        Form Scope_form;
        Form Generator_form;
        System.Timers.Timer GUITimer;

        public Instrulab()
        {
            InitializeComponent();
            comms = new Comms_thread();
            comm_th = new Thread(new ThreadStart(comms.run));
            comm_th.Start();
            GUITimer = new System.Timers.Timer(50);
            GUITimer.Elapsed += new ElapsedEventHandler(Update_GUI);
            
        }

        //invoke updating of GUI during searching of devices
        private void Update_GUI(object sender, ElapsedEventArgs e)
        {
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            switch (comms.get_searching_state()) { 
                case 0:
                    GUITimer.Stop();
                    break;
                case 1:
                    this.toolStripProgressBar.Value = comms.get_progress();
                    this.toolStripStatusLabel.Text = "Searching in progress";
                    break;
                case 2:
                    this.toolStripProgressBar.Value = 0;
                    if (comms.get_num_of_devices() == 0)
                    {
                        this.toolStripStatusLabel.Text = "No device was found";
                    }
                    else { 
                        this.toolStripStatusLabel.Text = "Searching done";
                            this.btn_connect.Enabled = true;

                            for (int i = 0; i < comms.get_num_of_devices(); i++)
                            {
                                this.listBox_devices.Items.Add(comms.get_dev_names()[i]);
                            }
                            this.listBox_devices.SelectedIndex = 0;
                    }
                    break;
            }
        }

        private void btn_scope_open_Click(object sender, EventArgs e)
        {
            if (Scope_form==null || Scope_form.IsDisposed)
            {
                Scope_form = new Scope();
                Scope_form.Show();
            }
            else {
                Scope_form.BringToFront();
            }
                
        }

        private void btn_gen_open_Click(object sender, EventArgs e)
        {
            if (Generator_form == null || Generator_form.IsDisposed)
            {
                Generator_form = new Generator();
                Generator_form.Show();
            }
            else {
                Generator_form.BringToFront();
            }

        }

        private void Instrulab_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Scope_form != null)
            {
                Scope_form.Close();
            }
            if (Generator_form != null)
            {
                Generator_form.Close();
            }
            if (comm_th.IsAlive) {
                comms.stop();
                while (comm_th.IsAlive) {
                    Thread.Yield();
                }
            }
        }

        private void exitInstrulabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btn_scan_Click(object sender, EventArgs e)
        {
            comms.find_devices_request();
            GUITimer.Start();
        }

        private void btn_connect_Click(object sender, EventArgs e)
        {
            if (this.btn_connect.Text.Equals("Connect"))
            {
                string dev = (string)this.listBox_devices.SelectedItem;

                if (dev == null)
                {
                    MessageBox.Show("You have to select device first", "No device selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    if (dev[4] == ':')
                    {
                        dev = dev.Substring(0, 4);
                    }
                    else
                    {
                        dev = dev.Substring(0, 5);
                    }
                    comms.connect_device(dev);
                  //  this.toolStripStatusLabel_status.Text = "Connecting to " + dev;
                   // this.mode = Paint_mode.Mode.CONNECTING;

                }
            }
        }
    }
}
