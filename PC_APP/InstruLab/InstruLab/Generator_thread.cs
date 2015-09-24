using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InstruLab
{
    class Generator_thread
    {
        private bool Run = true;

        public void run()
        {
            while (Run)
            {

            }
        }

        public void stop()
        {
            this.Run = false;
        }
    }
}
