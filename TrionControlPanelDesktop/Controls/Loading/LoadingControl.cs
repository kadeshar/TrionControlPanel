using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TrionControlPanelDesktop.Controls
{
    public partial class LoadingControl : UserControl
    {
        public LoadingControl()
        {
            Dock = DockStyle.Fill;
            InitializeComponent();
        }
        public string LoadingText
        {
            get => label1.Text;
            set => label1.Text = value;
        }
    }
}
