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
    public partial class Scope : Form
    {
        Thread scope_th;
        Scope_thread scope;

        public Scope()
        {
            InitializeComponent();
            scope = new Scope_thread();
            scope_th = new Thread(new ThreadStart(scope.run));
            scope_th.Start();
        }

        private void Scope_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (scope_th.IsAlive) {
                scope.stop();
                while (scope_th.IsAlive) {
                    Thread.Yield();
                }
            }
        }
    }
}
