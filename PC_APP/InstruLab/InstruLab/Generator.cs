using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Timers;
using ZedGraph;

namespace InstruLab
{
    public partial class Generator : Form
    {
        //Thread gen_th;
        Device device;
        System.Timers.Timer signalTimer;
        System.Timers.Timer dataSendingTimer;

        int semaphoreTimeout = 5000;

        private enum SIGNAL_TYPE { SINE,SQUARE,SAW,ARB };


        private bool bestFreqFit = true;
        private bool customLeng = false;

        private bool frequencyJoin = false;

        private bool khz_ch1=false;
        private bool khz_ch2 = false;
        private double freq_ch1 = 0; 
        private double freq_ch2 = 0;
        private double ampl_ch1 = 0;
        private double ampl_ch2 = 0;
        private double phase_ch1 = 0;
        private double phase_ch2 = 0;
        private double duty_ch1 = 50;
        private double duty_ch2 = 50;
        private double offset_ch1 = 0;
        private double offset_ch2 = 0;

        private int signal_leng_ch1 = 0;
        private int signal_leng_ch2 = 0;

        private int signal_leng = 0;

        private double last_sum = 0;
        private int divider_ch1 = 0;
        private int divider_ch2 = 0;

        private SIGNAL_TYPE signalType_ch1 = SIGNAL_TYPE.SINE;
        private SIGNAL_TYPE signalType_ch2 = SIGNAL_TYPE.SINE;
        private double[] signal_ch1; 
        private double[] signal_ch2;

        private double[] time_ch1;
        private double[] time_ch2;

        private int actual_channels = 1;

        public GraphPane channel1Pane;
        public GraphPane channel2Pane;

        private Queue<Message> gen_q = new Queue<Message>();
        Message messg;

        const int DATA_BLOCK = 32;
        int toSend = 0;
        int sent = 0;
        int index = 0;
        int actualSend = 0;
        private bool generating = false;
        int sendingChannel;

        double realFreq_ch1=0;
        double realFreq_ch2 = 0;

        public Generator(Device dev)
        {
            InitializeComponent();
            zedGraphControl_gen_ch1.MasterPane[0].IsFontsScaled = false;
            zedGraphControl_gen_ch1.MasterPane[0].Title.IsVisible = false;
            zedGraphControl_gen_ch1.MasterPane[0].XAxis.MajorGrid.IsVisible = true;
            zedGraphControl_gen_ch1.MasterPane[0].XAxis.Title.IsVisible = false;
            zedGraphControl_gen_ch1.MasterPane[0].XAxis.IsVisible = false;

            zedGraphControl_gen_ch1.MasterPane[0].YAxis.MajorGrid.IsVisible = true;
            zedGraphControl_gen_ch1.MasterPane[0].YAxis.Title.IsVisible = false;

            zedGraphControl_gen_ch2.MasterPane[0].IsFontsScaled = false;
            zedGraphControl_gen_ch2.MasterPane[0].Title.IsVisible = false;
            zedGraphControl_gen_ch2.MasterPane[0].XAxis.MajorGrid.IsVisible = true;
            zedGraphControl_gen_ch2.MasterPane[0].XAxis.Title.IsVisible = false;
            zedGraphControl_gen_ch2.MasterPane[0].XAxis.IsVisible = false;

            zedGraphControl_gen_ch2.MasterPane[0].YAxis.MajorGrid.IsVisible = true;
            zedGraphControl_gen_ch2.MasterPane[0].YAxis.Title.IsVisible = false;

            channel1Pane = zedGraphControl_gen_ch1.GraphPane;
            channel2Pane = zedGraphControl_gen_ch2.GraphPane;

            this.device = dev;
            this.trackBar_ampl_ch1.Maximum = dev.genCfg.VRef;
            this.trackBar_ampl_ch2.Maximum = dev.genCfg.VRef;
            this.trackBar_ampl_ch1.Value = dev.genCfg.VRef / 2;
            this.trackBar_ampl_ch2.Value = dev.genCfg.VRef / 2;
            this.textBox_ampl_ch1.Text = (dev.genCfg.VRef / 2).ToString();
            this.textBox_ampl_ch2.Text = (dev.genCfg.VRef / 2).ToString();

            this.trackBar_offset_ch1.Maximum = dev.genCfg.VRef;
            this.trackBar_offset_ch2.Maximum = dev.genCfg.VRef;
            this.trackBar_offset_ch1.Value = dev.genCfg.VRef / 2;
            this.trackBar_offset_ch2.Value = dev.genCfg.VRef / 2;
            this.trackBar_offset_ch1.Text = (dev.genCfg.VRef / 2).ToString();
            this.trackBar_offset_ch2.Text = (dev.genCfg.VRef / 2).ToString();

            freq_ch1 = trackBar_freq_ch1.Value/10;
            freq_ch2 = trackBar_freq_ch2.Value/10;

            validate_control_ch2();

            signalTimer = new System.Timers.Timer(200);
            signalTimer.Elapsed += new ElapsedEventHandler(Update_signal);
            signalTimer.Start();
            
            dataSendingTimer = new System.Timers.Timer(5);
            dataSendingTimer.Elapsed += new ElapsedEventHandler(data_sending);
            
            
        }

        private void data_sending(object sender, ElapsedEventArgs e)
        {
            if (gen_q.Count > 0)
            {
                messg = gen_q.Dequeue();
                if (messg == null)
                {
                    return;
                }
                switch (messg.GetRequest())
                {
                    case Message.MsgRequest.GEN_NEXT:
                        
                        if (toSend == 0)
                        {
                            if (sendingChannel == 1 && actual_channels == 2) {
                                toSend = signal_ch2.Length;
                                sent = 0;
                                index = 0;
                                actualSend = 0;
                                sendingChannel = 2;
                                send_next(signal_ch2, 2);
                            }
                            else if (sendingChannel == actual_channels) {
                                gen_get_freq();
                                Thread.Sleep(10);
                                gen_start();
                            }
                        }
                        else {
                            if (sendingChannel == 2)
                            {
                                send_next(signal_ch2, 2);
                            }
                            else {
                                send_next(signal_ch1, 1);
                            }
                        }
                        break;
                    case Message.MsgRequest.GEN_OK:
                        generating = true;
                        this.Invalidate();
                        break;
                    case Message.MsgRequest.GEN_FRQ:
                        if(messg.GetMessage().Equals(Commands.CHANNELS_1)){
                            this.realFreq_ch1 = (double)messg.GetNum() / signal_leng_ch1;
                        }
                        else if (messg.GetMessage().Equals(Commands.CHANNELS_2)) {
                            this.realFreq_ch2 = (double)messg.GetNum() / signal_leng_ch2;
                        }
                        this.Invalidate();
                        break;

                        
                }
            }
        }

        public void add_message(Message msg)
        {
            this.gen_q.Enqueue(msg);
        }

        private void Update_signal(object sender, ElapsedEventArgs e)
        {
            double sum = signalType_ch1.GetHashCode() + signalType_ch2.GetHashCode() + freq_ch1 + freq_ch2 + ampl_ch1 + ampl_ch2 + phase_ch1 + phase_ch2 + duty_ch1 + duty_ch2 + offset_ch1 + offset_ch2 + signal_leng + actual_channels;
            sum = bestFreqFit ? sum+1 : sum;
            sum = customLeng ? sum+2 : sum;
            sum = khz_ch1 ? sum+4 : sum;
            sum = khz_ch2 ? sum+8 : sum;
            sum = generating ? sum + 16 : sum;
            sum = frequencyJoin ? sum + 32 : sum;

            if (sum != last_sum) {
                last_sum = sum;

                if (!generating)
                {
                    calculate_signal_lengths();
                    generate_signals();
                    paint_signals();
                }
                else {
                    double tmpFreq = freq_ch1*signal_leng_ch1;
                    if (frequencyJoin) {
                        gen_stop();
                    }
                    tmpFreq = khz_ch1 ? tmpFreq * 1000 : tmpFreq;
                    set_frequency((int)tmpFreq, 1);

                    if (actual_channels == 2) {
                        tmpFreq = freq_ch2 * signal_leng_ch2;
                        tmpFreq = khz_ch2 ? tmpFreq * 1000 : tmpFreq;
                        set_frequency((int)tmpFreq, 2);
                    }
                    if (frequencyJoin) {
                        gen_start();
                    }

                    gen_get_freq();
                }
                this.Invalidate();
            }
        }

        private void paint_signals() {
            //plot signal
            channel1Pane.CurveList.Clear();
            LineItem curve;
            curve = channel1Pane.AddCurve("", time_ch1, signal_ch1, Color.Red, SymbolType.Diamond);
            curve.Line.IsSmooth = false;
            curve.Line.SmoothTension = 0.5F;
            curve.Line.IsAntiAlias = false;
            curve.Line.IsOptimizedDraw = true;
            curve.Symbol.Size = 0;

            channel1Pane.XAxis.Scale.MaxAuto = false;
            channel1Pane.XAxis.Scale.MinAuto = false;
            channel1Pane.YAxis.Scale.MaxAuto = false;
            channel1Pane.YAxis.Scale.MinAuto = false;

            channel1Pane.XAxis.Scale.Max = time_ch1[time_ch1.Length - 1]+time_ch1[1];
            channel1Pane.XAxis.Scale.Min = 0;
            channel1Pane.YAxis.Scale.Max = (double)(device.genCfg.VRef) / 1000;
            channel1Pane.YAxis.Scale.Min = 0;

            if (actual_channels == 2)
            {
                channel2Pane.CurveList.Clear();
                curve = channel2Pane.AddCurve("", time_ch2, signal_ch2, Color.Blue, SymbolType.Diamond);
                curve.Line.IsSmooth = false;
                curve.Line.SmoothTension = 0.5F;
                curve.Line.IsAntiAlias = false;
                curve.Line.IsOptimizedDraw = true;
                curve.Symbol.Size = 0;

                channel2Pane.XAxis.Scale.MaxAuto = false;
                channel2Pane.XAxis.Scale.MinAuto = false;
                channel2Pane.YAxis.Scale.MaxAuto = false;
                channel2Pane.YAxis.Scale.MinAuto = false;

                channel2Pane.XAxis.Scale.Max = time_ch2[time_ch2.Length - 1] + time_ch2[1];
                channel2Pane.XAxis.Scale.Min = 0;
                channel2Pane.YAxis.Scale.Max = (double)(device.genCfg.VRef) / 1000;
                channel2Pane.YAxis.Scale.Min = 0;
            }
        }

        public void generate_signals() {
            //generate signals
            for (int i = 1; i <= actual_channels; i++)
            {
                SIGNAL_TYPE tmpSigType;
                double[] tmpSignal;
                double[] tmpTime;
                double tmpAmpl;
                double tmpPhase;
                double tmpDuty;
                double tmpOffset;
                int tmpDiv;
                int shift;
                if (i == 1)
                {
                    tmpSignal = new double[signal_leng_ch1];
                    tmpSigType = signalType_ch1;
                    tmpAmpl = ampl_ch1;
                    tmpPhase = phase_ch1;
                    tmpDuty = duty_ch1;
                    tmpOffset = offset_ch1;
                    tmpDiv = divider_ch1;
                    
                }
                else
                {
                    tmpSignal = new double[signal_leng_ch2];
                    tmpSigType = signalType_ch2;
                    tmpAmpl = ampl_ch2;
                    tmpPhase = phase_ch2;
                    tmpDuty = duty_ch2;
                    tmpOffset = offset_ch2;
                    tmpDiv = divider_ch2;
                }

                tmpTime = new double[tmpSignal.Length];
                for (int j = 0; j < tmpSignal.Length; j++)
                {
                    tmpTime[j] = (double)j * tmpDiv / device.systemCfg.PeriphClock;
                }

                switch (tmpSigType)
                {
                    case SIGNAL_TYPE.SINE:
                        for (int j = 0; j < tmpSignal.Length; j++)
                        {
                            tmpSignal[j] = (tmpAmpl / 1000 * Math.Sin(2 * Math.PI * j / tmpSignal.Length + tmpPhase * Math.PI / 180) + tmpOffset / 1000);
                        }
                        break;
                    case SIGNAL_TYPE.SQUARE:
                        shift = (int)(tmpPhase / 360 * tmpSignal.Length);
                        for (int j = 0; j < tmpSignal.Length; j++)
                        {
                            if (j < tmpDuty / 100  * tmpSignal.Length )
                            {
                                tmpSignal[(j + shift) % tmpSignal.Length] = tmpOffset / 1000 - tmpAmpl / 1000;
                            }else{
                                tmpSignal[(j + shift) % tmpSignal.Length] = tmpOffset / 1000 + tmpAmpl / 1000;
                            }
                        }
                        break;
                    case SIGNAL_TYPE.SAW:
                        shift = (int)(tmpPhase / 360 * tmpSignal.Length);
                        for (int j = 0; j < tmpSignal.Length; j++)
                        {
                            if (j > tmpSignal.Length * tmpDuty / 100)
                            {
                                tmpSignal[(j + shift) % tmpSignal.Length] = (tmpOffset - tmpAmpl + tmpAmpl * 2 - tmpAmpl * 2 / (tmpSignal.Length - (tmpDuty / 100 * tmpSignal.Length)) * (j - (tmpSignal.Length * tmpDuty / 100))) / 1000;
                            }
                            else
                            {
                                tmpSignal[(j + shift) % tmpSignal.Length] = (tmpOffset - tmpAmpl + tmpAmpl * 2 / (tmpDuty / 100 * tmpSignal.Length) * j) / 1000;
                            }
                        }
                        break;


                }

                if (i == 1)
                {
                    signal_ch1 = tmpSignal;
                    time_ch1 = tmpTime;
                }
                else
                {
                    signal_ch2 = tmpSignal;
                    time_ch2 = tmpTime;
                }
            }
        }

        public void calculate_signal_lengths() {
            int tclk = device.systemCfg.PeriphClock;
            //estimate length and divider
            /*
             * *Best frequency fit*
             * 1 - Estimate minimal possible divider (sampling frequency or max length of signal)
             * 2 - Calculate desired signal length for current divider
             * 3 - Round signal length and calculate error 
             * 4 - Increment divider and calculate error again while error small enough or signal length too small
             */
            if (bestFreqFit)
            {
                
                double tmp_freq = checkBox_khz_ch1.Checked ? freq_ch1 * 1000 : freq_ch1;

                int divA = tclk / device.genCfg.maxSamplingFrequency;
                int divB = (int)(tclk / tmp_freq / (device.genCfg.BufferLength/2) * actual_channels);
                int div = divA > divB ? divA : divB;
                double error;
                double minimalError;
                int bestDiv = 0;
                int bestLeng = 0;
                double tmpSigLeng = 0; ;

                for (int i = 1; i <= actual_channels; i++)
                {
                    error = double.MaxValue;
                    minimalError = double.MaxValue;
                    if (i == 2)
                    {
                        tmp_freq = checkBox_khz_ch2.Checked ? freq_ch2 * 1000 : freq_ch2;
                        divB = (int)Math.Round((double)tclk / tmp_freq / (device.genCfg.BufferLength / 2) * actual_channels);
                        div = divA > divB ? divA : divB;
                    }

                    int iter = 0;
                    while (error > 0)
                    {
                        tmpSigLeng = tclk / tmp_freq / div;
                        error = Math.Abs(tmp_freq - (double)(tclk) / (div * (int)Math.Round(tmpSigLeng)));

                        if (tmpSigLeng-0.0001 > (device.genCfg.BufferLength / 2 / actual_channels))
                        {
                            div++;
                            iter++;
                            continue;
                        }
                        if (error < minimalError)
                        {
                            bestDiv = div;
                            bestLeng = (int)Math.Round(tmpSigLeng);
                            minimalError = error;
                        }

                        if (error < 0.01)
                        {
                            break;
                        }

                        if (tmpSigLeng < (device.genCfg.BufferLength / 2 / actual_channels / 4) && iter > 5)
                        {
                            break;
                        }
                        div++;
                        iter++;
                    }

                    if (i == 1)
                    {
                        signal_leng_ch1 = bestLeng;
                        divider_ch1 = bestDiv;
                    }
                    else
                    {
                        signal_leng_ch2 = bestLeng;
                        divider_ch2 = bestDiv;
                    }
                }
            }
            else
            { //isn't best frequency fit 
                double tmp_freq_ch1 = checkBox_khz_ch1.Checked ? freq_ch1 * 1000 : freq_ch1;
                double tmp_freq_ch2 = checkBox_khz_ch2.Checked ? freq_ch2 * 1000 : freq_ch2;
                if (customLeng)
                {
                    int.TryParse(toolStripTextBox_signal_leng.Text, out signal_leng_ch1);
                    int.TryParse(toolStripTextBox_signal_leng.Text, out signal_leng_ch2);

                    int divA = tclk / device.genCfg.maxSamplingFrequency;
                    int divB = (int)(tclk / tmp_freq_ch1 / signal_leng_ch1);
                    divider_ch1 = divA > divB ? divA : divB;

                    divA = tclk / device.genCfg.maxSamplingFrequency;
                    divB = (int)(tclk / tmp_freq_ch2 / signal_leng_ch2);
                    divider_ch2 = divA > divB ? divA : divB;
                }
                else
                {
                    int div = tclk / device.genCfg.maxSamplingFrequency;
                    double leng = (double)tclk / tmp_freq_ch1 / div;
                    if (leng > device.genCfg.BufferLength / 2 / actual_channels)
                    {
                        signal_leng_ch1 = device.genCfg.BufferLength / 2 / actual_channels;
                        divider_ch1 = (int)(tclk / tmp_freq_ch1 / signal_leng_ch1);
                    }
                    else
                    {
                        signal_leng_ch1 = (int)leng;
                        divider_ch1 = (int)(tclk / tmp_freq_ch1 / signal_leng_ch1);
                    }

                    leng = (double)tclk / tmp_freq_ch2 / div;
                    if (leng > device.genCfg.BufferLength / 2 / actual_channels)
                    {
                        signal_leng_ch2 = device.genCfg.BufferLength / 2 / actual_channels;
                        divider_ch2 = (int)(tclk / tmp_freq_ch2 / signal_leng_ch2);
                    }
                    else
                    {
                        signal_leng_ch2 = (int)leng;
                        divider_ch2 = (int)(tclk / tmp_freq_ch2 / signal_leng_ch2);
                    }
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            if (this.generating) {
                button_gen_control.Enabled = true;
                label_status.BackColor = Color.LightGreen;
                label_status_gen.Text = "Generating";
                this.button_gen_control.Enabled=true;
                this.button_gen_control.Text = "Disable";
            }
            zedGraphControl_gen_ch1.Refresh();
            zedGraphControl_gen_ch2.Refresh();
            if (generating)
            {
                label_real_freq_ch1_title.Text = "Real frequency";
                label_real_freq_ch1.Text = Math.Round((double)realFreq_ch1, 2).ToString()+ " Hz";
                if (actual_channels == 2)
                {
                    label_real_freq_ch2.Text = Math.Round((double)realFreq_ch2, 2).ToString()+" Hz";
                }
            }
            else
            {
                label_real_freq_ch1_title.Text = "Estimate freq.";
                label_real_freq_ch1.Text = Math.Round((double)(device.systemCfg.PeriphClock) / signal_leng_ch1 / divider_ch1, 2).ToString()+" Hz";
                label_sig_leng_ch1.Text = signal_leng_ch1.ToString();// +" " + divider_ch1.ToString();
                if (actual_channels == 2)
                {
                    label_sig_leng_ch2.Text = signal_leng_ch2.ToString();// +" " + divider_ch2.ToString();
                    label_real_freq_ch2.Text = Math.Round((double)(device.systemCfg.PeriphClock) / signal_leng_ch2 / divider_ch2, 2).ToString()+ "Hz";
                }
            }
            

            
            base.OnPaint(e);
        }

        //communication with device
        public void gen_start()
        {
            device.takeCommsSemaphore(semaphoreTimeout);
            device.send(Commands.GENERATOR + ":" + Commands.START + ";");
            device.giveCommsSemaphore();
        }

        public void gen_stop()
        {
            device.takeCommsSemaphore(semaphoreTimeout);
            device.send(Commands.GENERATOR + ":" + Commands.STOP + ";");
            device.giveCommsSemaphore();
        }
        
        public void set_data_length(string chan,int len) {
            device.takeCommsSemaphore(semaphoreTimeout);
            device.send(Commands.GENERATOR + ":" + chan + " ");
            device.send_short((int)(len));
            device.send(";");
            device.giveCommsSemaphore();
        }

        public void set_num_of_channels(string chann)
        {
            device.takeCommsSemaphore(semaphoreTimeout);
            device.send(Commands.GENERATOR + ":" + Commands.CHANNELS + " " + chann + ";");
            device.giveCommsSemaphore();
        }

        public void set_frequency(int freq, int chann) {
            device.takeCommsSemaphore(semaphoreTimeout);
            device.send(Commands.GENERATOR + ":" + Commands.SAMPLING_FREQ + " ");
            device.send_int(freq*256 + chann);
            device.send(";");
            device.giveCommsSemaphore();
        }

        public void gen_get_freq() {
            device.takeCommsSemaphore(semaphoreTimeout);
            device.send(Commands.GENERATOR + ":" + Commands.GEN_GET_REAL_SMP_FREQ + ";");
            device.giveCommsSemaphore();
        }

        public void send_next(double[] data, int chann)
        {
            device.takeCommsSemaphore(semaphoreTimeout);
            int tmpData;
            device.send(Commands.GENERATOR + ":" + Commands.GEN_DATA + " ");
            if (toSend > DATA_BLOCK)
            {
                actualSend = DATA_BLOCK;
            }
            else
            {
                actualSend = toSend;
            }
            device.send_int((index / 256) + (index % 256) * 256 + (actualSend * 256 * 256) + (chann * 256 * 256 * 256));
            device.send(":");
            for (int i = 0; i < actualSend; i++)
            {
                tmpData = (int)Math.Round(data[sent + i] / device.genCfg.VRef * 1000 * (Math.Pow(2, device.genCfg.dataDepth) - 1));
                if (tmpData > Math.Pow(2, device.genCfg.dataDepth) - 1) {
                    tmpData = (int)Math.Pow(2, device.genCfg.dataDepth) - 1;
                }
                else if (tmpData < 0) {
                    tmpData = 0;
                }
                device.send_short_2byte(tmpData);
            }
            sent += actualSend;
            toSend -= actualSend;
            index += actualSend;
            device.send(";");
            device.giveCommsSemaphore();
        }


        // END communication with device

       

        private void Generator_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (generating) {
                gen_stop();
            }
        }


        private void maximumPossibleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            customLeng = false;
            bestFreqFit = false;
            validateFreqFit();
        }

        private void bestFreqFitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            customLeng = false;
            bestFreqFit = true;
            validateFreqFit();
        }

        private void customToolStripMenuItem_Click(object sender, EventArgs e)
        {
            customLeng = true;
            bestFreqFit = false;
            validateFreqFit();
        }

        private void validateFreqFit() {
            if (!customLeng)
            {
                maximumPossibleToolStripMenuItem.Checked = !bestFreqFit ? true : false;
                bestFreqFitToolStripMenuItem.Checked = bestFreqFit ? true : false;
                customToolStripMenuItem.Checked = false;
            }
            else {
                maximumPossibleToolStripMenuItem.Checked =  false;
                bestFreqFitToolStripMenuItem.Checked = false;
                customToolStripMenuItem.Checked = true;
            }

            if (!bestFreqFit && !customLeng)
            {
                signal_leng = device.genCfg.BufferLength / 2 / actual_channels;
            }
        }


        // track-bar and tex-box functions

        private void trackBar_freq_ch1_ValueChanged(object sender, EventArgs e)
        {
            if (this.trackBar_freq_ch1.Value < 0.1)
            {
                freq_ch1 = 0.1;
            }
            else
            {
                freq_ch1 = ((double)(this.trackBar_freq_ch1.Value) / 10);
            }
            if (frequencyJoin) {
                this.trackBar_freq_ch2.Value = this.trackBar_freq_ch1.Value;
            }
            this.textBox_freq_ch1.Text = freq_ch1.ToString();
        }

        private void textBox_freq_ch1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                validate_text_freq_ch1();
            }
        }

        private void textBox_freq_ch1_Leave(object sender, EventArgs e)
        {
            validate_text_freq_ch1();
        }

        private void validate_text_freq_ch1()
        {
            try
            {
                Double val = Double.Parse(this.textBox_freq_ch1.Text);
                if (val < 1)
                {
                    if (khz_ch1)
                    {
                        khz_ch1 = false;
                        checkBox_khz_ch1.Checked = false;
                        val = val * 1000;
                    }
                    else
                    {
                        throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                    }
                }
                else if (val > 1000)
                {
                    if (!khz_ch1)
                    {
                        khz_ch1 = true;
                        checkBox_khz_ch1.Checked = true;
                        val = val / 1000;
                    }
                    else
                    {
                        throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                    }
                }
                this.trackBar_freq_ch1.Value = (int)(val * 10);
                freq_ch1 = val;
                
            }
            catch (Exception ex)
            {
            }
            finally { 
                this.textBox_freq_ch1.Text = freq_ch1.ToString();
            }
        }

        private void checkBox_khz_ch1_CheckedChanged(object sender, EventArgs e)
        {
            khz_ch1 = checkBox_khz_ch1.Checked;
        }

        private void textBox_freq_ch2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                validate_text_freq_ch2();
            }
        }

        private void textBox_freq_ch2_Leave(object sender, EventArgs e)
        {
            validate_text_freq_ch2();
        }

        private void trackBar_freq_ch2_ValueChanged(object sender, EventArgs e)
        {
            if (this.trackBar_freq_ch2.Value < 0.1)
            {
                freq_ch2 = 0.1;
            }
            else
            {
                freq_ch2 = ((double)(this.trackBar_freq_ch2.Value) / 10);
            }
            if (frequencyJoin)
            {
                this.trackBar_freq_ch1.Value = this.trackBar_freq_ch2.Value;
            }
            this.textBox_freq_ch2.Text = freq_ch2.ToString();
        }

        private void checkBox_khz_ch2_CheckedChanged(object sender, EventArgs e)
        {
            khz_ch2 = checkBox_khz_ch2.Checked;
        }

        private void validate_text_freq_ch2()
        {
            try
            {
                Double val = Double.Parse(this.textBox_freq_ch2.Text);
                if (val < 1)
                {
                    if (khz_ch1)
                    {
                        khz_ch2 = false;
                        checkBox_khz_ch2.Checked = false;
                        val = val * 1000;
                    }
                    else
                    {
                        throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                    }
                }
                else if (val > 1000)
                {
                    if (!khz_ch2)
                    {
                        khz_ch2 = true;
                        checkBox_khz_ch2.Checked = true;
                        val = val / 1000;
                    }
                    else
                    {
                        throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                    }
                }
                this.trackBar_freq_ch2.Value = (int)(val * 10);
                freq_ch2 = val;

            }
            catch (Exception ex)
            {
            }
            finally
            {
                this.textBox_freq_ch2.Text = freq_ch2.ToString();
            }
        }



        private void trackBar_ampl_ch1_ValueChanged(object sender, EventArgs e)
        {
            ampl_ch1 = ((double)(this.trackBar_ampl_ch1.Value));
            this.textBox_ampl_ch1.Text = ampl_ch1.ToString();
        }

        private void textBox_ampl_ch1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                validate_text_ampl_ch1();
            }
        }

        private void textBox_ampl_ch1_Leave(object sender, EventArgs e)
        {
            validate_text_ampl_ch1();
        }

        private void validate_text_ampl_ch1()
        {
            try
            {
                Double val = Double.Parse(this.textBox_ampl_ch1.Text);
                if (val > device.genCfg.VRef)
                {
                    throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                }
                this.trackBar_ampl_ch1.Value = (int)(val);
                ampl_ch1 = val;
            }
            catch (Exception ex)
            {
            }
            finally
            {
                this.textBox_ampl_ch1.Text = ampl_ch1.ToString();
            }
        }



        private void trackBar_ampl_ch2_ValueChanged(object sender, EventArgs e)
        {
            ampl_ch2 = ((double)(this.trackBar_ampl_ch2.Value));
            this.textBox_ampl_ch2.Text = ampl_ch2.ToString();
        }

        private void textBox_ampl_ch2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                validate_text_ampl_ch2();
            }
        }

        private void textBox_ampl_ch2_Leave(object sender, EventArgs e)
        {
            validate_text_ampl_ch2();
        }

        private void validate_text_ampl_ch2()
        {
            try
            {
                Double val = Double.Parse(this.textBox_ampl_ch2.Text);
                if (val > device.genCfg.VRef)
                {
                    throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                }
                this.trackBar_ampl_ch2.Value = (int)(val);
                ampl_ch2 = val;
            }
            catch (Exception ex)
            {
            }
            finally
            {
                this.textBox_ampl_ch2.Text = ampl_ch2.ToString();
            }
        }


        private void trackBar_phase_ch1_ValueChanged(object sender, EventArgs e)
        {
            phase_ch1 = ((double)(this.trackBar_phase_ch1.Value) / 10);
            this.textBox_phase_ch1.Text = phase_ch1.ToString();
        }

        private void textBox_phase_ch1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                validate_text_phase_ch1();
            }
        }

        private void textBox_phase_ch1_Leave(object sender, EventArgs e)
        {
            validate_text_phase_ch1();
        }

        private void validate_text_phase_ch1()
        {
            try
            {
                Double val = Double.Parse(this.textBox_phase_ch1.Text);
                if (val > 360)
                {
                    throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                }
                this.trackBar_phase_ch1.Value = (int)(val * 10);
                phase_ch1 = val;

            }
            catch (Exception ex)
            {
            }
            finally
            {
                this.textBox_phase_ch1.Text = phase_ch1.ToString();
            }
        }


        private void trackBar_phase_ch2_ValueChanged(object sender, EventArgs e)
        {
            phase_ch2 = ((double)(this.trackBar_phase_ch2.Value) / 10);
            this.textBox_phase_ch2.Text = phase_ch2.ToString();
        }

        private void textBox_phase_ch2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                validate_text_phase_ch2();
            }
        }

        private void textBox_phase_ch2_Leave(object sender, EventArgs e)
        {
            validate_text_phase_ch2();
        }

        private void validate_text_phase_ch2()
        {
            try
            {
                Double val = Double.Parse(this.textBox_phase_ch2.Text);
                if (val > 360)
                {
                    throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                }
                this.trackBar_phase_ch2.Value = (int)(val * 10);
                phase_ch2 = val;

            }
            catch (Exception ex)
            {
            }
            finally
            {
                this.textBox_phase_ch2.Text = phase_ch2.ToString();
            }
        }



        private void trackBar_duty_ch1_ValueChanged(object sender, EventArgs e)
        {
            duty_ch1 = ((double)(this.trackBar_duty_ch1.Value) / 10);
            this.textBox_duty_ch1.Text = duty_ch1.ToString();
        }

        private void textBox_duty_ch1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                validate_text_duty_ch1();
            }
        }

        private void textBox_duty_ch1_Leave(object sender, EventArgs e)
        {
            validate_text_duty_ch1();
        }

        private void validate_text_duty_ch1()
        {
            try
            {
                Double val = Double.Parse(this.textBox_duty_ch1.Text);
                if (val > 100)
                {
                    throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                }
                this.trackBar_duty_ch1.Value = (int)(val * 10);
                duty_ch1 = val;

            }
            catch (Exception ex)
            {
            }
            finally
            {
                this.textBox_duty_ch1.Text = duty_ch1.ToString();
            }
        }



        private void trackBar_dut_ch2_ValueChanged(object sender, EventArgs e)
        {
            duty_ch2 = ((double)(this.trackBar_duty_ch2.Value) / 10);
            this.textBox_duty_ch2.Text = duty_ch2.ToString();
        }

        private void textBox_duty_ch2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                validate_text_duty_ch2();
            }
        }

        private void textBox_duty_ch2_Leave(object sender, EventArgs e)
        {
            validate_text_duty_ch2();
        }

        private void validate_text_duty_ch2()
        {
            try
            {
                Double val = Double.Parse(this.textBox_duty_ch2.Text);
                if (val > 100)
                {
                    throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                }
                this.trackBar_duty_ch2.Value = (int)(val * 10);
                duty_ch1 = val;

            }
            catch (Exception ex)
            {
            }
            finally
            {
                this.textBox_duty_ch2.Text = duty_ch2.ToString();
            }
        }


        private void trackBar_offset_ch1_ValueChanged(object sender, EventArgs e)
        {
            offset_ch1 = ((double)(this.trackBar_offset_ch1.Value));
            this.textBox_offset_ch1.Text = offset_ch1.ToString();
        }

        private void textBox_offset_ch1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                validate_text_offset_ch1();
            }
        }

        private void textBox_offset_ch1_Leave(object sender, EventArgs e)
        {
            validate_text_offset_ch1();
        }

        private void validate_text_offset_ch1()
        {
            try
            {
                Double val = Double.Parse(this.textBox_offset_ch1.Text);
                if (val > device.genCfg.VRef)
                {
                    throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                }
                this.trackBar_offset_ch1.Value = (int)(val);
                offset_ch1 = val;
            }
            catch (Exception ex)
            {
            }
            finally
            {
                this.textBox_offset_ch1.Text = offset_ch1.ToString();
            }
        }


        private void trackBar_offset_ch2_ValueChanged(object sender, EventArgs e)
        {
            offset_ch2 = ((double)(this.trackBar_offset_ch2.Value));
            this.textBox_offset_ch2.Text = offset_ch2.ToString();
        }

        private void textBox_offset_ch2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                validate_text_offset_ch2();
            }
        }

        private void textBox_offset_ch2_Leave(object sender, EventArgs e)
        {
            validate_text_offset_ch2();
        }

        private void validate_text_offset_ch2()
        {
            try
            {
                Double val = Double.Parse(this.textBox_offset_ch2.Text);
                if (val > device.genCfg.VRef)
                {
                    throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                }
                this.trackBar_offset_ch2.Value = (int)(val);
                offset_ch2 = val;
            }
            catch (Exception ex)
            {
            }
            finally
            {
                this.textBox_offset_ch2.Text = offset_ch2.ToString();
            }
        }


        private void toolStripTextBox_signal_leng_TextChanged(object sender, EventArgs e)
        {
            try
            {
                int val = int.Parse(this.toolStripTextBox_signal_leng.Text);
                if ((actual_channels == 1 && val > device.genCfg.BufferLength / 2) || (actual_channels == 2 && val > device.genCfg.BufferLength / 2 / 2))
                {
                    throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                }
                signal_leng = val;
            }
            catch (Exception ex)
            {
            }
            toolStripTextBox_signal_leng.Text = signal_leng.ToString();
        }

        private void checkBox_enable_ch2_CheckedChanged(object sender, EventArgs e)
        {
            actual_channels = checkBox_enable_ch2.Checked ? 2 : 1;
            validate_control_ch2();
            if (!bestFreqFit && !customLeng)
            {
                signal_leng = device.genCfg.BufferLength / 2 / actual_channels;
            }
        }

        private void validate_control_ch2() {
            zedGraphControl_gen_ch2.Enabled = actual_channels == 2 ? true : false;
            trackBar_ampl_ch2.Enabled = actual_channels == 2 ? true : false;
            trackBar_duty_ch2.Enabled = actual_channels == 2 ? true : false;
            trackBar_freq_ch2.Enabled = actual_channels == 2 ? true : false;
            trackBar_offset_ch2.Enabled = actual_channels == 2 ? true : false;
            trackBar_phase_ch2.Enabled = actual_channels == 2 ? true : false;

            textBox_ampl_ch2.Enabled = actual_channels == 2 ? true : false;
            textBox_duty_ch2.Enabled = actual_channels == 2 ? true : false;
            textBox_freq_ch2.Enabled = actual_channels == 2 ? true : false;
            textBox_offset_ch2.Enabled = actual_channels == 2 ? true : false;
            textBox_phase_ch2.Enabled = actual_channels == 2 ? true : false;
            checkBox_khz_ch2.Enabled = actual_channels == 2 ? true : false;

            label_real_freq_ch2_title.Enabled = actual_channels == 2 ? true : false;
            label_real_freq_ch2.Enabled = actual_channels == 2 ? true : false;
            label_sig_leng_ch2.Enabled = actual_channels == 2 ? true : false;
            label9.Enabled = actual_channels == 2 ? true : false;

        }

        private void checkBox_join_frequencies_CheckedChanged(object sender, EventArgs e)
        {
            frequencyJoin = this.checkBox_join_frequencies.Checked;
            if (frequencyJoin) {
                trackBar_freq_ch2.Value = trackBar_freq_ch1.Value;
            }
        }

        private void radioButton_sine_ch1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_sine_ch1.Checked)
            {
                signalType_ch1 = SIGNAL_TYPE.SINE;
            }
        }

        private void radioButton_square_ch1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_square_ch1.Checked)
            {
                signalType_ch1 = SIGNAL_TYPE.SQUARE;
            }
        }

        private void radioButton_saw_ch1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_saw_ch1.Checked)
            {
                signalType_ch1 = SIGNAL_TYPE.SAW;
            }
        }

        private void radioButton_arb_ch1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_arb_ch1.Checked)
            {
                signalType_ch1 = SIGNAL_TYPE.ARB;
            }
        }

        private void radioButton_sine_ch2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_sine_ch2.Checked)
            {
                signalType_ch2 = SIGNAL_TYPE.SINE;
            }
        }

        private void radioButton_square_ch2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_square_ch2.Checked)
            {
                signalType_ch2 = SIGNAL_TYPE.SQUARE;
            }
        }

        private void radioButton_saw_ch2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_saw_ch2.Checked)
            {
                signalType_ch2 = SIGNAL_TYPE.SAW;
            }
        }

        private void radioButton_arb_ch2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_arb_ch2.Checked)
            {
                signalType_ch2 = SIGNAL_TYPE.ARB;
            }
        }


        // END track-bar and tex-box functions


        private void button_gen_control_Click(object sender, EventArgs e)
        {
            if (button_gen_control.Text.Equals("Enable") && button_gen_control.Enabled==true)
            {
                this.button_gen_control.Enabled = false;
                label_status.BackColor = Color.Yellow;
                label_status_gen.Text = "Updating";
                //send data to generator;
                if (actual_channels == 2) { 
                    set_data_length(Commands.DATA_LENGTH_CH1, signal_leng_ch1);
                    set_data_length(Commands.DATA_LENGTH_CH2, signal_leng_ch2);
                    set_num_of_channels(Commands.CHANNELS_2);
                }else{
                    set_num_of_channels(Commands.CHANNELS_1);
                    set_data_length(Commands.DATA_LENGTH_CH1, signal_leng_ch1);
                }


                if (checkBox_khz_ch1.Checked)
                {
                    set_frequency((int)(device.systemCfg.PeriphClock / signal_leng_ch1 / divider_ch1 * signal_leng_ch1), 1);
                }
                else {
                    set_frequency((int)(device.systemCfg.PeriphClock / signal_leng_ch1 / divider_ch1 * signal_leng_ch1), 1);
                }

                if (checkBox_khz_ch2.Checked && actual_channels==2)
                {
                    set_frequency((int)(device.systemCfg.PeriphClock / signal_leng_ch2 / divider_ch2 * signal_leng_ch2), 2);
                }
                else if (actual_channels == 2)
                {
                    set_frequency((int)(device.systemCfg.PeriphClock / signal_leng_ch2 / divider_ch2 * signal_leng_ch2), 2);
                }

                toSend = signal_ch1.Length;
                sent = 0;
                index = 0;
                actualSend = 0;
                sendingChannel = 1;
                dataSendingTimer.Start();

                Thread.Sleep(10);
                send_next(signal_ch1, 1);

                
                

            }
            else {
                gen_stop();
                this.button_gen_control.Text = "Enable";
                this.label_status_gen.Text = "Idle";
                label_status.BackColor = Color.Red;
                generating = false;
            }
        }


 








        



     

 




    }
}
