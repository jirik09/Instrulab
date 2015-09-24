using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace InstruLab
{
    public partial class Generator : Form
    {
        Thread gen_th;
        Generator_thread gen;
        public Generator()
        {
            InitializeComponent();
            gen = new Generator_thread();
            gen_th = new Thread(new ThreadStart(gen.run));
            gen_th.Start();
        }

        private void Generator_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (gen_th.IsAlive)
            {
                gen.stop();
                while (gen_th.IsAlive)
                {
                    Thread.Yield();
                }
            }
        }




    }
}
