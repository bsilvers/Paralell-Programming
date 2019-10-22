using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsAsyncApp
{
    public partial class Form1 : Form
    {
        public int CalculateValue()
        {
            Thread.Sleep(5000);
            return 123;
        }

        public Form1()
        {
            InitializeComponent();
        }

    }
}
