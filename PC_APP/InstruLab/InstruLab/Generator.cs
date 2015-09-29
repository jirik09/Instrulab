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
        Device device;

        public Generator(Device dev)
        {
            InitializeComponent();
            this.device = dev;
        }

        private void Generator_FormClosing(object sender, FormClosingEventArgs e)
        {

        }




    }
}
