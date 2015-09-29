using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Timers;
using ZedGraph;

namespace InstruLab
{

    public partial class Scope : Form
    {
        //Thread scope_th;
        //Scope_thread scope;
        Device device;
        InstruLab.Device.ScopeConfig_def ScopeDevice;
        System.Timers.Timer GUITimer;

        private Queue<Message> scope_q = new Queue<Message>();
        Message messg;
        
        public enum triggerEdge_def { RISE, FALL };
        public enum mode_def {IDLE, NORMAL, AUTO, SINGLE };

        public triggerEdge_def triggeredge = triggerEdge_def.RISE;
        public double triggerLevel;
        public double pretrigger;
        public int adcRes;
        public int numSamples;
        public int actualCahnnels;
        private int triggerChannel;

        private double[] signal_ch1;
        private double[] signal_ch2;
        private double[] signal_ch3;
        private double[] signal_ch4;

        private int[] gain=new int[4] {1,1,1,1};
        public int[] offset = new int[4] { 0, 0, 0, 0 };
        private int selectedRange = 0;
        private int selectedChannelVolt = 0;


        private bool interpolation = true;
        private bool showPoints = false;
        private float smoothing = 0.5F;
        


        public GraphPane scopePane;

        private string status_text="";


        double scale=1;
        double horPosition=0.5;

        
    

        public Scope(Device dev)
        {
            InitializeComponent();
            zedGraphControl_scope.MasterPane[0].IsFontsScaled = false;
            zedGraphControl_scope.MasterPane[0].Title.IsVisible = false;
            zedGraphControl_scope.MasterPane[0].XAxis.MajorGrid.IsVisible = true;
            zedGraphControl_scope.MasterPane[0].XAxis.Title.IsVisible = false;

            zedGraphControl_scope.MasterPane[0].YAxis.MajorGrid.IsVisible = true;
            zedGraphControl_scope.MasterPane[0].YAxis.Title.IsVisible = false;
            this.device = dev;
            ScopeDevice=device.scopeCfg;
            set_scope_default();

            validate_radio_btns();
            validate_menu();

            scopePane = zedGraphControl_scope.GraphPane;
            
            GUITimer = new System.Timers.Timer(20);
            GUITimer.Elapsed += new ElapsedEventHandler(Update_GUI);
            GUITimer.Start();

            Thread.Sleep(10);
            scope_start();
        }

        private void Update_GUI(object sender, ElapsedEventArgs e)
        {
            if (scope_q.Count > 0)
            {
                messg = scope_q.Dequeue();
                switch (messg.GetRequest())
                {
                    case Message.MsgRequest.SCOPE_NEW_DATA:
                        status_text = "";
                        process_signals();
                        paint_signals();                     
                        break;

                    case Message.MsgRequest.SCOPE_TRIGGERED:
                        status_text = "Trig";
                        break;

                    case Message.MsgRequest.CHANGE_ZOOM:
                        process_signals();
                        paint_signals(); 
                        break;
                    case Message.MsgRequest.CHANGE_GAIN:
                        paint_signals();
                        break;
                    case Message.MsgRequest.SCOPE_WAIT:
                        scopePane.CurveList.Clear();
                        status_text = "Wait";
                        break;
                }
                this.Invalidate();
            }
            
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            zedGraphControl_scope.Refresh();
            this.label_scope_status.Text = status_text;
            base.OnPaint(e);
        }

        private void Scope_FormClosing(object sender, FormClosingEventArgs e)
        {
            scope_stop();
            GUITimer.Stop();
        }

        public void add_message(Message msg)
        {
            this.scope_q.Enqueue(msg);
        }





        //
        // sending commands to scope
        //

        public void scope_start()
        {
            device.send(Commands.SCOPE + ":" + Commands.START + ";");
        }

        public void scope_stop()
        {
            device.send(Commands.SCOPE + ":" + Commands.STOP + ";");
        }
        public void scope_next()
        {
            device.send(Commands.SCOPE + ":" + Commands.SCOPE_NEXT + ";");
        }


        public void set_data_depth(string dataDepth) {
            device.send(Commands.SCOPE + ":" + Commands.SCOPE_DATA_DEPTH + " " + dataDepth + ";");
        }

        public void set_num_of_samples(string numSmp) {
            device.send(Commands.SCOPE + ":" + Commands.DATA_LENGTH + " " + numSmp + ";");
        }


        public void set_sampling_freq(string smpFreq)
        {
            device.send(Commands.SCOPE + ":" + Commands.SAMPLING_FREQ + " " + smpFreq+ ";");
        }

        public void set_num_of_channels(string chann) {
            device.send(Commands.SCOPE + ":" + Commands.CHANNELS + " " + chann + ";");
        }

        public void set_trigger_channel(string chann)
        {
            device.send(Commands.SCOPE + ":" + Commands.SCOPE_TRIG_CHANNEL + " " + chann + ";");
        }

        public void set_trigger_mode(mode_def mod)
        {
            switch (mod)
            {
                case mode_def.AUTO:
                    device.send(Commands.SCOPE + ":" + Commands.SCOPE_TRIG_MODE + " " + Commands.MODE_AUTO + ";");
                    break;
                case mode_def.NORMAL:
                    device.send(Commands.SCOPE + ":" + Commands.SCOPE_TRIG_MODE + " " + Commands.MODE_NORMAL + ";");
                    break;
                case mode_def.SINGLE:
                    device.send(Commands.SCOPE + ":" + Commands.SCOPE_TRIG_MODE + " " + Commands.MODE_SINGLE + ";");
                    break;
            }
            if (device.get_scope_mode() == mode_def.SINGLE)
            {
                Thread.Sleep(20);
                scope_next();
            }
            device.set_scope_mode(mod);           
        }

        public void set_trigger_edge_fall() {
            device.send(Commands.SCOPE + ":" + Commands.SCOPE_TRIG_EDGE + " " + Commands.EDGE_FALLING + ";");
        }
        public void set_trigger_edge_rise() {
            device.send(Commands.SCOPE + ":" + Commands.SCOPE_TRIG_EDGE + " " + Commands.EDGE_RISING + ";");
        }

        public void set_prettriger(double pre) {
            device.send(Commands.SCOPE + ":" + Commands.SCOPE_PRETRIGGER + " ");
            device.send_short((int)(pre*65536));
            device.send(";");
        }

        public void set_trigger_level(double level) {
            device.send(Commands.SCOPE + ":" + Commands.SCOPE_TRIG_LEVEL + " ");
            device.send_short((int)(level * 65536));
            device.send(";");
        }



        









        public bool validate_buffer_usage() {
            if (adcRes > 8)
            {
                if (2 * numSamples * actualCahnnels <= ScopeDevice.maxBufferLength)
                {
                    return true;
                }
            }
            else {
                if (numSamples * actualCahnnels <= ScopeDevice.maxBufferLength)
                {
                    return true;
                }
            }
            return false;
        }

        public void set_scope_default()
        {
            //must be same as in window designer!!!
            set_trigger_mode(mode_def.AUTO);

            set_sampling_freq(Commands.FREQ_1K);
            device.scopeCfg.sampligFreq = 1000;

            triggeredge = triggerEdge_def.RISE;

            triggerLevel = 0.5;
            set_trigger_level(triggerLevel);

            pretrigger = 0.5;
            set_prettriger(pretrigger);

            adcRes = 12;
            set_data_depth(Commands.DATA_DEPTH_12B);

            numSamples=100;
            set_num_of_samples(Commands.SAMPLES_100);

            actualCahnnels = 1;
            set_num_of_channels(Commands.CHANNELS_1);

            triggerChannel = 1;
            set_trigger_channel(Commands.CHANNELS_1);

        }



        //
        // Callbacks
        //

        private void bitsToolStripMenuItem_12bit_Click(object sender, EventArgs e)
        {
            if (this.bitsToolStripMenuItem_12bit.Checked)
            {
                int tmpAdcRes = adcRes;
                this.adcRes = 12;
                if (validate_buffer_usage())
                {
                    set_data_depth(Commands.DATA_DEPTH_12B);
                }
                else
                {
                    adcRes = tmpAdcRes;
                    show_buffer_err_message();
                }
                update_data_depth_menu();
            }else{
                this.bitsToolStripMenuItem_12bit.Checked = true;
            }
        }
        private void bitsToolStripMenuItem_8bit_Click(object sender, EventArgs e)
        {
            if (this.bitsToolStripMenuItem_8bit.Checked)
            {
                int tmpAdcRes = adcRes;
                this.adcRes = 8;
                if (validate_buffer_usage())
                {
                    set_data_depth(Commands.DATA_DEPTH_8B);
                }
                else
                {
                    adcRes = tmpAdcRes;
                    show_buffer_err_message();
                }
                update_data_depth_menu();
            }else {
                this.bitsToolStripMenuItem_8bit.Checked = true;
            }
        }

        
        private void checkBox_trig_normal_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_trig_normal.Checked)
            {
                set_trigger_mode(mode_def.NORMAL);
                this.checkBox_trig_auto.Checked = false;
                this.checkBox_trig_single.Checked = false;
            }
        }

        private void checkBox_trig_auto_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_trig_auto.Checked)
            {
                set_trigger_mode(mode_def.AUTO);
                this.checkBox_trig_normal.Checked = false;
                this.checkBox_trig_single.Checked = false;
            }
        }
        private void checkBox_trig_single_Click(object sender, EventArgs e)
        {
            this.checkBox_trig_single.Checked = true;
            if (device.get_scope_mode() == mode_def.SINGLE)
            {
                scope_next();
            }
            else
            {
                set_trigger_mode(mode_def.SINGLE);
                this.checkBox_trig_auto.Checked = false;
                this.checkBox_trig_normal.Checked = false;
            }
            add_message(new Message(Message.MsgRequest.SCOPE_WAIT));
        }

        private void checkBox_trig_rise_Click(object sender, EventArgs e)
        {
            if (this.checkBox_trig_rise.Checked)
            {
                this.checkBox_trig_fall.Checked = false;
                set_trigger_edge_rise();
            }
            this.checkBox_trig_rise.Checked = true;
        }
        private void checkBox_trig_fall_Click(object sender, EventArgs e)
        {
            if (this.checkBox_trig_fall.Checked)
            {
                this.checkBox_trig_rise.Checked = false;
                set_trigger_edge_fall();
            }
            this.checkBox_trig_fall.Checked = true;
        }

        //signal zoom
        private void trackBar_zoom_ValueChanged(object sender, EventArgs e)
        {
            scale = 1.0 - (double)(this.trackBar_zoom.Value) / (this.trackBar_zoom.Maximum - this.trackBar_zoom.Minimum + 10);
            add_message(new Message(Message.MsgRequest.CHANGE_ZOOM));
        }
        private void trackBar_position_ValueChanged(object sender, EventArgs e)
        {
            horPosition = (double)(this.trackBar_position.Value) / (this.trackBar_position.Maximum - this.trackBar_position.Minimum);
            add_message(new Message(Message.MsgRequest.CHANGE_ZOOM));
        }

        private void radioButton_1k_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButton_1k.Checked)
            {
                set_sampling_freq(Commands.FREQ_1K);
                device.scopeCfg.sampligFreq = 1000;
            }
        }

        private void radioButton_2k_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButton_2k.Checked)
            {
                set_sampling_freq(Commands.FREQ_2K);
                device.scopeCfg.sampligFreq = 2000;
            }
        }

        private void radioButton_5k_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButton_5k.Checked)
            {
                set_sampling_freq(Commands.FREQ_5K);
                device.scopeCfg.sampligFreq = 5000;
            }
        }

        private void radioButton_10k_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButton_10k.Checked)
            {
                set_sampling_freq(Commands.FREQ_10K);
                device.scopeCfg.sampligFreq = 10000;
            }
        }

        private void radioButton_20k_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButton_20k.Checked)
            {
                set_sampling_freq(Commands.FREQ_20K);
                device.scopeCfg.sampligFreq = 20000;
            }
        }

        private void radioButton_50k_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButton_50k.Checked)
            {
                set_sampling_freq(Commands.FREQ_50K);
                device.scopeCfg.sampligFreq = 50000;
            }
        }

        private void radioButton_100k_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButton_100k.Checked)
            {
                set_sampling_freq(Commands.FREQ_100K);
                device.scopeCfg.sampligFreq = 100000;
            }
        }

        private void radioButton_200k_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButton_200k.Checked)
            {
                set_sampling_freq(Commands.FREQ_200K); ;
                device.scopeCfg.sampligFreq = 200000;
            }
        }

        private void radioButton_500k_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButton_500k.Checked)
            {
                set_sampling_freq(Commands.FREQ_500K);
                device.scopeCfg.sampligFreq = 500000;
            }
        }

        private void radioButton_1m_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButton_1m.Checked)
            {
                set_sampling_freq(Commands.FREQ_1M);
                device.scopeCfg.sampligFreq = 1000000;
            }
        }

        private void radioButton_2m_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButton_2m.Checked)
            {
                set_sampling_freq(Commands.FREQ_2M);
                device.scopeCfg.sampligFreq = 2000000;
            }
        }

        private void radioButton_5m_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButton_5m.Checked)
            {
                set_sampling_freq(Commands.FREQ_5M);
                device.scopeCfg.sampligFreq = 5000000;
            }
        }

        private void radioButton_1x_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_1x.Checked)
            {
                this.gain[selectedChannelVolt] = 1;
                add_message(new Message(Message.MsgRequest.CHANGE_GAIN));
            }
        }

        private void radioButton_2x_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_2x.Checked)
            {
                this.gain[selectedChannelVolt] = 2;
                add_message(new Message(Message.MsgRequest.CHANGE_GAIN));
            }
        }

        private void radioButton_5x_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_5x.Checked)
            {
                this.gain[selectedChannelVolt] = 5;
                add_message(new Message(Message.MsgRequest.CHANGE_GAIN));
            }
        }

        private void radioButton_10x_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_10x.Checked)
            {
                this.gain[selectedChannelVolt] = 10;
                add_message(new Message(Message.MsgRequest.CHANGE_GAIN));
            }
        }

        private void radioButton_20x_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_20x.Checked)
            {
                this.gain[selectedChannelVolt] = 20;
                add_message(new Message(Message.MsgRequest.CHANGE_GAIN));
            }
        }

        private void radioButton_50x_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_50x.Checked)
            {
                this.gain[selectedChannelVolt] = 50;
                add_message(new Message(Message.MsgRequest.CHANGE_GAIN));
            }
        }

        private void radioButton_100x_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_100x.Checked)
            {
                this.gain[selectedChannelVolt] = 100;
                add_message(new Message(Message.MsgRequest.CHANGE_GAIN));
            }
        }

        private void radioButton_200x_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_200x.Checked)
            {
                this.gain[selectedChannelVolt] = 200;
                add_message(new Message(Message.MsgRequest.CHANGE_GAIN));
            }
        }

        private void radioButton_500x_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_500x.Checked)
            {
                this.gain[selectedChannelVolt] = 500;
                add_message(new Message(Message.MsgRequest.CHANGE_GAIN));
            }
        }

        private void trackBar_vol_level_ValueChanged(object sender, EventArgs e)
        {
            offset[selectedChannelVolt] = trackBar_vol_level.Value - 500;
            add_message(new Message(Message.MsgRequest.CHANGE_ZOOM));
        }

        private void channelToolStripMenuItem_1ch_Click(object sender, EventArgs e)
        {
            if (this.channelToolStripMenuItem_1ch.Checked)
            {
                int tmpActualCahnnels = actualCahnnels;
                this.actualCahnnels = 1;
                if (validate_buffer_usage())
                {
                    set_num_of_channels(Commands.CHANNELS_1);
                    validate_radio_btns();
                    if (triggerChannel >= 1) {
                        triggerChannel = 1;
                        set_trigger_channel(Commands.CHANNELS_1);
                        this.radioButton_trig_ch1.Checked = true;
                    }
                }
                else
                {
                    actualCahnnels = tmpActualCahnnels;
                    show_buffer_err_message();
                }
                update_channels_menu();
            }
            else
            {
                this.channelToolStripMenuItem_1ch.Checked = true;
            }
        }

        private void channelToolStripMenuItem_2ch_Click(object sender, EventArgs e)
        {
            if (this.channelToolStripMenuItem_2ch.Checked)
            {
                int tmpActualCahnnels = actualCahnnels;
                this.actualCahnnels = 2;
                if (validate_buffer_usage())
                {
                    set_num_of_channels(Commands.CHANNELS_2);
                    validate_radio_btns();
                    if (triggerChannel >= 2)
                    {
                        triggerChannel = 2;
                        set_trigger_channel(Commands.CHANNELS_2);
                        this.radioButton_trig_ch2.Checked = true;
                    }
                }
                else
                {
                    actualCahnnels = tmpActualCahnnels;
                    show_buffer_err_message();
                }
                update_channels_menu();
            }
            else
            {
                this.channelToolStripMenuItem_2ch.Checked = true;
            }
        }

        private void channelToolStripMenuItem_3ch_Click(object sender, EventArgs e)
        {
            if (this.channelToolStripMenuItem_3ch.Checked)
            {
                int tmpActualCahnnels = actualCahnnels;
                this.actualCahnnels = 3;
                if (validate_buffer_usage())
                {
                    set_num_of_channels(Commands.CHANNELS_3);
                    validate_radio_btns();
                    if (triggerChannel >= 3)
                    {
                        triggerChannel = 3;
                        set_trigger_channel(Commands.CHANNELS_3);
                        this.radioButton_trig_ch3.Checked = true;
                    }
                }
                else
                {
                    actualCahnnels = tmpActualCahnnels;
                    show_buffer_err_message();
                }
                update_channels_menu();
            }
            else
            {
                this.channelToolStripMenuItem_3ch.Checked = true;
            }
        }

        private void channelToolStripMenuItem_4ch_Click(object sender, EventArgs e)
        {
            if (this.channelToolStripMenuItem_4ch.Checked)
            {
                int tmpActualCahnnels = actualCahnnels;
                this.actualCahnnels = 4;
                if (validate_buffer_usage())
                {
                    set_num_of_channels(Commands.CHANNELS_4);
                    validate_radio_btns();
                    if (triggerChannel >= 4)
                    {
                        triggerChannel = 4;
                        set_trigger_channel(Commands.CHANNELS_4);
                        this.radioButton_trig_ch4.Checked = true;
                    }
                }
                else
                {
                    actualCahnnels = tmpActualCahnnels;
                    show_buffer_err_message();
                }
                update_channels_menu();
            }
            else
            {
                this.channelToolStripMenuItem_4ch.Checked = true;
            }
        }

        private void radioButton_volt_ch1_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButton_volt_ch1.Checked)
            {
                this.selectedChannelVolt = 0;
                redrawVolt();
            }
        }

        private void radioButton_volt_ch2_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButton_volt_ch2.Checked)
            {
                this.selectedChannelVolt = 1;
                redrawVolt();
            }
        }

        private void radioButton_volt_ch3_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButton_volt_ch3.Checked)
            {
                this.selectedChannelVolt = 2;
                redrawVolt();
            }
        }

        private void radioButton_volt_ch4_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButton_volt_ch4.Checked)
            {
                this.selectedChannelVolt = 3;
                redrawVolt();
            }
        }

        private void range0ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.range0ToolStripMenuItem.Checked)
            {
                selectedRange = 0;
                this.range1ToolStripMenuItem.Checked = false;
                this.range2ToolStripMenuItem.Checked = false;
                this.range3ToolStripMenuItem.Checked = false;
            }
            else
            {
                this.range0ToolStripMenuItem.Checked = true;
            }
        }

        private void range1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.range1ToolStripMenuItem.Checked)
            {
                selectedRange = 1;
                this.range0ToolStripMenuItem.Checked = false;
                this.range2ToolStripMenuItem.Checked = false;
                this.range3ToolStripMenuItem.Checked = false;
            }
            else
            {
                this.range1ToolStripMenuItem.Checked = true;
            }
        }

        private void range2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.range2ToolStripMenuItem.Checked)
            {
                selectedRange = 2;
                this.range0ToolStripMenuItem.Checked = false;
                this.range1ToolStripMenuItem.Checked = false;
                this.range3ToolStripMenuItem.Checked = false;
            }
            else
            {
                this.range2ToolStripMenuItem.Checked = true;
            }
        }

        private void range3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.range3ToolStripMenuItem.Checked)
            {
                selectedRange = 3;
                this.range0ToolStripMenuItem.Checked = false;
                this.range1ToolStripMenuItem.Checked = false;
                this.range2ToolStripMenuItem.Checked = false;
            }
            else
            {
                this.range3ToolStripMenuItem.Checked = true;
            }
        }

        private void radioButton_trig_ch1_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButton_trig_ch1.Checked)
            {
                triggerChannel = 1;
                set_trigger_channel(Commands.CHANNELS_1);
            }
        }

        private void radioButton_trig_ch2_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButton_trig_ch2.Checked)
            {
                triggerChannel = 2;
                set_trigger_channel(Commands.CHANNELS_2);
            }
        }

        private void radioButton_trig_ch3_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButton_trig_ch3.Checked)
            {
                triggerChannel = 3;
                set_trigger_channel(Commands.CHANNELS_3);
            }
        }

        private void radioButton_trig_ch4_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButton_trig_ch4.Checked)
            {
                triggerChannel = 4;
                set_trigger_channel(Commands.CHANNELS_4);
            }
        }
        private void maskedTextBox_pretrig_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                try
                {
                    double val = double.Parse(this.maskedTextBox_pretrig.Text);
                    if (val > 100)
                    {
                        throw new System.ArgumentException("Parameter cannot be greather then 100", "original");
                    }
                    pretrigger = val / 100;
                    set_prettriger(pretrigger);

                }
                catch (Exception ex)
                {
                    this.maskedTextBox_pretrig.Text = (pretrigger * 100).ToString();
                }
            }
        }

        private void maskedTextBox_trig_level_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                try
                {
                    double val = double.Parse(this.maskedTextBox_trig_level.Text);
                    if (val > 100)
                    {
                        throw new System.ArgumentException("Parameter cannot be greather then 100", "original");
                    }
                    triggerLevel = val / 100;
                    set_trigger_level(triggerLevel);

                }
                catch (Exception ex)
                {
                    this.maskedTextBox_trig_level.Text = (pretrigger * 100).ToString();
                }
            }
        }

        private void interpolateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            interpolation = this.interpolateToolStripMenuItem.Checked;
        }

        private void showPointsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showPoints = this.showPointsToolStripMenuItem.Checked;
        }

        private void ToolStripMenuItem_100smp_Click(object sender, EventArgs e)
        {
            if (this.ToolStripMenuItem_100smp.Checked)
            {
                int tmpNumSamples = numSamples;
                this.numSamples = 100;
                if (validate_buffer_usage())
                {
                    set_num_of_samples(Commands.SAMPLES_100);
                }
                else
                {
                    numSamples = tmpNumSamples;
                    show_buffer_err_message();
                }
                update_data_len_menu();
            }
            else
            {
                this.ToolStripMenuItem_100smp.Checked = true;
            }
        }

        private void ToolStripMenuItem_200smp_Click(object sender, EventArgs e)
        {
            if (this.ToolStripMenuItem_200smp.Checked)
            {
                int tmpNumSamples = numSamples;
                this.numSamples = 200;
                if (validate_buffer_usage())
                {
                    set_num_of_samples(Commands.SAMPLES_200);
                }
                else
                {
                    numSamples = tmpNumSamples;
                    show_buffer_err_message();
                }
                update_data_len_menu();
            }
            else
            {
                this.ToolStripMenuItem_200smp.Checked = true;
            }
        }

        private void ToolStripMenuItem_500smp_Click(object sender, EventArgs e)
        {
            if (this.ToolStripMenuItem_500smp.Checked)
            {
                int tmpNumSamples = numSamples;
                this.numSamples = 500;
                if (validate_buffer_usage())
                {
                    set_num_of_samples(Commands.SAMPLES_500);
                }
                else
                {
                    numSamples = tmpNumSamples;
                    show_buffer_err_message();
                }
                update_data_len_menu();
            }
            else
            {
                this.ToolStripMenuItem_500smp.Checked = true;
            }
        }

        private void ToolStripMenuItem_1ksmp_Click(object sender, EventArgs e)
        {
            if (this.ToolStripMenuItem_1ksmp.Checked)
            {
                int tmpNumSamples = numSamples;
                this.numSamples = 1000;
                if (validate_buffer_usage())
                {
                    set_num_of_samples(Commands.SAMPLES_1K);
                }
                else
                {
                    numSamples = tmpNumSamples;
                    show_buffer_err_message();
                }
                update_data_len_menu();
            }
            else
            {
                this.ToolStripMenuItem_1ksmp.Checked = true;
            }
        }

        private void ToolStripMenuItem_2ksmp_Click(object sender, EventArgs e)
        {
            if (this.ToolStripMenuItem_2ksmp.Checked)
            {
                int tmpNumSamples = numSamples;
                this.numSamples = 2000;
                if (validate_buffer_usage())
                {
                    set_num_of_samples(Commands.SAMPLES_2K);
                }
                else
                {
                    numSamples = tmpNumSamples;
                    show_buffer_err_message();
                }
                update_data_len_menu();
            }
            else
            {
                this.ToolStripMenuItem_2ksmp.Checked = true;
            }
        }

        private void ToolStripMenuItem_5ksmp_Click(object sender, EventArgs e)
        {
            if (this.ToolStripMenuItem_5ksmp.Checked)
            {
                int tmpNumSamples = numSamples;
                this.numSamples = 5000;
                if (validate_buffer_usage())
                {
                    set_num_of_samples(Commands.SAMPLES_5K);
                }
                else
                {
                    numSamples = tmpNumSamples;
                    show_buffer_err_message();
                }
                update_data_len_menu();
            }
            else
            {
                this.ToolStripMenuItem_5ksmp.Checked = true;
            }
        }

        private void ToolStripMenuItem_10ksmp_Click(object sender, EventArgs e)
        {
            if (this.ToolStripMenuItem_10ksmp.Checked)
            {
                int tmpNumSamples = numSamples;
                this.numSamples = 10000;
                if (validate_buffer_usage())
                {
                    set_num_of_samples(Commands.SAMPLES_10K);
                }
                else
                {
                    numSamples = tmpNumSamples;
                    show_buffer_err_message();
                }
                update_data_len_menu();
            }
            else
            {
                this.ToolStripMenuItem_10ksmp.Checked = true;
            }
        }

        private void ToolStripMenuItem_20ksmp_Click(object sender, EventArgs e)
        {
            if (this.ToolStripMenuItem_20ksmp.Checked)
            {
                int tmpNumSamples = numSamples;
                this.numSamples = 20000;
                if (validate_buffer_usage())
                {
                    set_num_of_samples(Commands.SAMPLES_20K);
                }
                else
                {
                    numSamples = tmpNumSamples;
                    show_buffer_err_message();
                }
                update_data_len_menu();
            }
            else
            {
                this.ToolStripMenuItem_20ksmp.Checked = true;
            }
        }

        private void ToolStripMenuItem_50ksmp_Click(object sender, EventArgs e)
        {
            if (this.ToolStripMenuItem_50ksmp.Checked)
            {
                int tmpNumSamples = numSamples;
                this.numSamples = 50000;
                if (validate_buffer_usage())
                {
                    set_num_of_samples(Commands.SAMPLES_50K);
                }
                else
                {
                    numSamples = tmpNumSamples;
                    show_buffer_err_message();
                }
                update_data_len_menu();
            }
            else
            {
                this.ToolStripMenuItem_50ksmp.Checked = true;
            }
        }

        private void ToolStripMenuItem_100ksmp_Click(object sender, EventArgs e)
        {
            if (this.ToolStripMenuItem_100ksmp.Checked)
            {
                int tmpNumSamples = numSamples;
                this.numSamples = 100000;
                if (validate_buffer_usage())
                {
                    set_num_of_samples(Commands.SAMPLES_100K);
                }
                else
                {
                    numSamples = tmpNumSamples;
                    show_buffer_err_message();
                }
                update_data_len_menu();
            }
            else
            {
                this.ToolStripMenuItem_100ksmp.Checked = true;
            }
        }

        private void button_volt_reset_chan_Click(object sender, EventArgs e)
        {
            gain[selectedChannelVolt] = 1;
            offset[selectedChannelVolt] = 0;
            this.trackBar_vol_level.Value = 500;
            redrawVolt();
        }

        private void button_volt_reset_all_Click(object sender, EventArgs e)
        {
            gain = new int[4] { 1, 1, 1, 1 };
            offset = new int[4] { 0, 0, 0, 0 };
            this.trackBar_vol_level.Value = 500;
            redrawVolt();
        }


        //end callbacks



        //
        // methodts to keep window actual
        //

        public void redrawVolt() {
            this.trackBar_vol_level.Value = offset[selectedChannelVolt] + 500;
            this.radioButton_1x.Checked = gain[selectedChannelVolt] == 1 ? true : false;
            this.radioButton_2x.Checked = gain[selectedChannelVolt] == 2 ? true : false;
            this.radioButton_5x.Checked = gain[selectedChannelVolt] == 5 ? true : false;
            this.radioButton_10x.Checked = gain[selectedChannelVolt] == 10 ? true : false;
            this.radioButton_20x.Checked = gain[selectedChannelVolt] == 20 ? true : false;
            this.radioButton_50x.Checked = gain[selectedChannelVolt] == 50 ? true : false;
            this.radioButton_100x.Checked = gain[selectedChannelVolt] == 100 ? true : false;
            this.radioButton_200x.Checked = gain[selectedChannelVolt] == 200 ? true : false;
            this.radioButton_500x.Checked = gain[selectedChannelVolt] == 500 ? true : false;

        }


        public void validate_radio_btns() {

            this.radioButton_trig_ch1.Enabled = actualCahnnels >= 1 ? true : false;
            this.radioButton_ver_cur_ch1.Enabled = actualCahnnels >= 1 ? true : false;
            this.radioButton_volt_ch1.Enabled = actualCahnnels >= 1 ? true : false;
            this.radioButton_hor_cur_ch1.Enabled = actualCahnnels >= 1 ? true : false;

            this.radioButton_trig_ch2.Enabled = actualCahnnels >= 2 ? true : false;
            this.radioButton_ver_cur_ch2.Enabled = actualCahnnels >= 2 ? true : false;
            this.radioButton_volt_ch2.Enabled = actualCahnnels >= 2 ? true : false;
            this.radioButton_hor_cur_ch2.Enabled = actualCahnnels >= 2 ? true : false;

            this.radioButton_trig_ch3.Enabled = actualCahnnels >= 3 ? true : false;
            this.radioButton_ver_cur_ch3.Enabled = actualCahnnels >= 3 ? true : false;
            this.radioButton_volt_ch3.Enabled = actualCahnnels >= 3 ? true : false;
            this.radioButton_hor_cur_ch3.Enabled = actualCahnnels >= 3 ? true : false;

            this.radioButton_trig_ch4.Enabled = actualCahnnels >= 4 ? true : false;
            this.radioButton_ver_cur_ch4.Enabled = actualCahnnels >= 4 ? true : false;
            this.radioButton_volt_ch4.Enabled = actualCahnnels >= 4 ? true : false;
            this.radioButton_hor_cur_ch4.Enabled = actualCahnnels >= 4 ? true : false;    
        }

        public void update_data_len_menu()
        {
            this.ToolStripMenuItem_100smp.Checked = numSamples == 100 ? true : false;
            this.ToolStripMenuItem_200smp.Checked = numSamples == 200 ? true : false;
            this.ToolStripMenuItem_500smp.Checked = numSamples == 500 ? true : false;
            this.ToolStripMenuItem_1ksmp.Checked = numSamples == 1000 ? true : false;
            this.ToolStripMenuItem_2ksmp.Checked = numSamples == 2000 ? true : false;
            this.ToolStripMenuItem_5ksmp.Checked = numSamples == 5000 ? true : false;
            this.ToolStripMenuItem_10ksmp.Checked = numSamples == 10000 ? true : false;
            this.ToolStripMenuItem_20ksmp.Checked = numSamples == 20000 ? true : false;
            this.ToolStripMenuItem_50ksmp.Checked = numSamples == 50000 ? true : false;
            this.ToolStripMenuItem_100ksmp.Checked = numSamples == 100000 ? true : false;
        }

        public void update_data_depth_menu() {
            this.bitsToolStripMenuItem_8bit.Checked = adcRes == 8 ? true : false;
            this.bitsToolStripMenuItem_12bit.Checked = adcRes == 12 ? true : false;
        }

        public void update_channels_menu(){
            this.channelToolStripMenuItem_1ch.Checked = this.actualCahnnels == 1 ? true : false;
            this.channelToolStripMenuItem_2ch.Checked = this.actualCahnnels == 2 ? true : false;
            this.channelToolStripMenuItem_3ch.Checked = this.actualCahnnels == 3 ? true : false;
            this.channelToolStripMenuItem_4ch.Checked = this.actualCahnnels == 4 ? true : false;
        }


        public void validate_menu()
        {
            if (ScopeDevice.maxNumChannels < 1) {
                this.channelToolStripMenuItem_1ch.Dispose();
            }
            if (ScopeDevice.maxNumChannels < 2)
            {
                this.channelToolStripMenuItem_2ch.Dispose();
            }
            if (ScopeDevice.maxNumChannels < 3)
            {
                this.channelToolStripMenuItem_3ch.Dispose();
            }
            if (ScopeDevice.maxNumChannels < 4)
            {
                this.channelToolStripMenuItem_4ch.Dispose();
            }
            
            if (ScopeDevice.maxBufferLength < 100) {
                this.ToolStripMenuItem_100smp.Dispose();
            }
            if (ScopeDevice.maxBufferLength < 200)
            {
                this.ToolStripMenuItem_200smp.Dispose();
            } if (ScopeDevice.maxBufferLength < 500)
            {
                this.ToolStripMenuItem_500smp.Dispose();
            } if (ScopeDevice.maxBufferLength < 1000)
            {
                this.ToolStripMenuItem_1ksmp.Dispose();
            } if (ScopeDevice.maxBufferLength < 2000)
            {
                this.ToolStripMenuItem_2ksmp.Dispose();
            } if (ScopeDevice.maxBufferLength < 5000)
            {
                this.ToolStripMenuItem_5ksmp.Dispose();
            } if (ScopeDevice.maxBufferLength < 10000)
            {
                this.ToolStripMenuItem_10ksmp.Dispose();
            } if (ScopeDevice.maxBufferLength < 20000)
            {
                this.ToolStripMenuItem_20ksmp.Dispose();
            } if (ScopeDevice.maxBufferLength < 50000)
            {
                this.ToolStripMenuItem_50ksmp.Dispose();
            } if (ScopeDevice.maxBufferLength < 100000)
            {
                this.ToolStripMenuItem_100ksmp.Dispose();
            }
           
            this.radioButton_1k.Enabled = ScopeDevice.maxSamplingFrequency >= 1000 ? true : false;
            this.radioButton_2k.Enabled = ScopeDevice.maxSamplingFrequency >= 2000 ? true : false;
            this.radioButton_5k.Enabled = ScopeDevice.maxSamplingFrequency >= 5000 ? true : false;
            this.radioButton_10k.Enabled = ScopeDevice.maxSamplingFrequency >= 10000 ? true : false;
            this.radioButton_20k.Enabled = ScopeDevice.maxSamplingFrequency >= 20000 ? true : false;
            this.radioButton_50k.Enabled = ScopeDevice.maxSamplingFrequency >= 50000 ? true : false;
            this.radioButton_100k.Enabled = ScopeDevice.maxSamplingFrequency >= 100000 ? true : false;
            this.radioButton_200k.Enabled = ScopeDevice.maxSamplingFrequency >= 200000 ? true : false;
            this.radioButton_500k.Enabled = ScopeDevice.maxSamplingFrequency >= 500000 ? true : false;
            this.radioButton_1m.Enabled = ScopeDevice.maxSamplingFrequency >= 1000000 ? true : false;
            this.radioButton_2m.Enabled = ScopeDevice.maxSamplingFrequency >= 2000000 ? true : false;
            this.radioButton_5m.Enabled = ScopeDevice.maxSamplingFrequency >= 5000000 ? true : false;


            if (ScopeDevice.ranges[0, 0] != 0 || ScopeDevice.ranges[1, 0] != 0)
            {
                this.range0ToolStripMenuItem.Text = "Default (" + ScopeDevice.ranges[0, 0] + "mV, " + ScopeDevice.ranges[1, 0] + "mV)";
            }

            if (ScopeDevice.ranges[0, 1] != 0 || ScopeDevice.ranges[1, 1] != 0)
            {
                this.range1ToolStripMenuItem.Text = "Range 1 (" + ScopeDevice.ranges[0, 1] + "mV, " + ScopeDevice.ranges[1, 1] + "mV)";
            }
            else {
                this.range1ToolStripMenuItem.Dispose();
            }

            if (ScopeDevice.ranges[0, 2] != 0 || ScopeDevice.ranges[1, 2] != 0)
            {
                this.range2ToolStripMenuItem.Text = "Range 2 (" + ScopeDevice.ranges[0, 2] + "mV, " + ScopeDevice.ranges[1, 2] + "mV)";
            }
            else {
                this.range2ToolStripMenuItem.Dispose();
            }

            if (ScopeDevice.ranges[0, 3] != 0 || ScopeDevice.ranges[1, 3] != 0)
            {
                this.range3ToolStripMenuItem.Text = "Range 3 (" + ScopeDevice.ranges[0, 3] + "mV, " + ScopeDevice.ranges[1, 3] + "mV)";
            }
            else {
                this.range3ToolStripMenuItem.Dispose();
            }

            if (ScopeDevice.maxNumChannels < 1) {
                this.toolStripMenuItem_ch1_meas.Dispose();
            }
            if (ScopeDevice.maxNumChannels < 2)
            {
                this.toolStripMenuItem_ch2_meas.Dispose();
            }
            if (ScopeDevice.maxNumChannels < 3)
            {
                this.toolStripMenuItem_ch3_meas.Dispose();
            }
            if (ScopeDevice.maxNumChannels < 4)
            {
                this.toolStripMenuItem_ch4_meas.Dispose();
            }

        }



        //scope painting and data processing

        public void process_signals()
        {
            for (int i = 0; i < device.scopeCfg.actualChannels; i++)
            {
                double scale = (device.scopeCfg.ranges[1, selectedRange] - device.scopeCfg.ranges[0, selectedRange]) / 1000 / Math.Pow(2, device.scopeCfg.actualRes) * gain[i];
                double off = (device.scopeCfg.ranges[1, selectedRange] - device.scopeCfg.ranges[0, selectedRange]) / 1000 * (double)offset[i] / 1000 * gain[i] * 2 + device.scopeCfg.ranges[0, selectedRange] / 1000;
                switch (i)
                {
                    case 0:
                        this.signal_ch1 = new double[device.scopeCfg.timeBase.Length];
                        for (int j = 0; j < device.scopeCfg.timeBase.Length; j++)
                        {
                            signal_ch1[j] = device.scopeCfg.samples[0, j]*scale+off;
                        }
                        break;
                    case 1:
                        this.signal_ch2 = new double[device.scopeCfg.timeBase.Length];
                        for (int j = 0; j < device.scopeCfg.timeBase.Length; j++)
                        {
                            signal_ch2[j] = device.scopeCfg.samples[1, j] * scale + off;
                        }
                        break;
                    case 2:
                        this.signal_ch3 = new double[device.scopeCfg.timeBase.Length];
                        for (int j = 0; j < device.scopeCfg.timeBase.Length; j++)
                        {
                            signal_ch3[j] = device.scopeCfg.samples[2, j] * scale + off;
                        }
                        break;
                    case 3:
                        this.signal_ch4 = new double[device.scopeCfg.timeBase.Length];
                        for (int j = 0; j < device.scopeCfg.timeBase.Length; j++)
                        {
                            signal_ch4[j] = device.scopeCfg.samples[3, j] * scale + off;
                        }
                        break;
                }
            }
        }


        public void paint_signals()
        {
            scopePane.CurveList.Clear();
            LineItem curve;
            if (device.scopeCfg.actualChannels >= 1)
            {
                curve = scopePane.AddCurve("", device.scopeCfg.timeBase, signal_ch1, Color.Red, SymbolType.Diamond);
                curve.Line.IsSmooth = interpolation;
                curve.Line.SmoothTension = smoothing;
                curve.Line.IsOptimizedDraw = true;
                curve.Symbol.Size = showPoints ? 4 : 0;
            }
            if (device.scopeCfg.actualChannels >= 2)
            {
                curve = scopePane.AddCurve("", device.scopeCfg.timeBase, signal_ch2, Color.Blue, SymbolType.Diamond);
                curve.Line.IsSmooth = interpolation;
                curve.Line.SmoothTension = smoothing;
                curve.Line.IsOptimizedDraw = true;
                curve.Symbol.Size = showPoints ? 4 : 0;
            }
            if (device.scopeCfg.actualChannels >= 3)
            {
                curve = scopePane.AddCurve("", device.scopeCfg.timeBase, signal_ch3, Color.DarkGreen, SymbolType.Diamond);
                curve.Line.IsSmooth = interpolation;
                curve.Line.SmoothTension = smoothing;
                curve.Line.IsOptimizedDraw = true;
                curve.Symbol.Size = showPoints ? 4 : 0;
            }
            if (device.scopeCfg.actualChannels >= 4)
            {
                curve = scopePane.AddCurve("", device.scopeCfg.timeBase, signal_ch4, Color.Blue, SymbolType.Diamond);
                curve.Line.IsSmooth = interpolation;
                curve.Line.SmoothTension = smoothing;
                curve.Line.IsOptimizedDraw = true;
                curve.Symbol.Size = showPoints ? 4 : 0;
            }


            scopePane.XAxis.Scale.MaxAuto = false;
            scopePane.XAxis.Scale.MinAuto = false;

            scopePane.YAxis.Scale.MaxAuto = false;
            scopePane.YAxis.Scale.MinAuto = false;

            double maxTime = device.scopeCfg.maxTime;
            double interval = scale * maxTime;
            double posmin = (interval / 2);
            double posScale = (maxTime - interval) / maxTime;

            double maxX = (maxTime) * horPosition * posScale + posmin + interval / 2;
            double minX = (maxTime) * horPosition * posScale + posmin - interval / 2;

            double maxY = device.scopeCfg.ranges[1, selectedRange] / 1000;
            double minY = device.scopeCfg.ranges[0, selectedRange] / 1000;

            scopePane.XAxis.Scale.Max = maxX;
            scopePane.XAxis.Scale.Min = minX;

            scopePane.YAxis.Scale.Max = maxY;
            scopePane.YAxis.Scale.Min = minY;

            scopePane.AxisChange();
            
            //zoom position
            PointPairList list1 = new PointPairList();
            list1.Add((device.scopeCfg.maxTime) * horPosition, maxY);
            curve = scopePane.AddCurve("", list1, Color.Red, SymbolType.TriangleDown);
            curve.Symbol.Size = 15;
            curve.Symbol.Fill.Color = Color.Red;
            curve.Symbol.Fill.IsVisible = true;
            
            //trigger time
            list1 = new PointPairList();
            list1.Add(((device.scopeCfg.maxTime) * pretrigger - (device.scopeCfg.maxTime / device.scopeCfg.timeBase.Length)), maxY);
            curve = scopePane.AddCurve("", list1, Color.Blue, SymbolType.TriangleDown);
            curve.Symbol.Size = 20;
            curve.Symbol.Fill.Color = Color.Blue;
            curve.Symbol.Fill.IsVisible = true;

            //triggerlevel
            list1 = new PointPairList();
            list1.Add(minX, triggerLevel*(maxY-minY));
            curve = scopePane.AddCurve("", list1, Color.Green, SymbolType.Diamond);
            curve.Symbol.Size = 15;
            curve.Symbol.Fill.Color = Color.Green;
            curve.Symbol.Fill.IsVisible = true;
        }


        





        public void show_buffer_err_message() {
            MessageBox.Show("Buffer usage error \r\nTry to decrease number of samples, data resolution or number of channels", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

 



        /*

         */








    }
}
