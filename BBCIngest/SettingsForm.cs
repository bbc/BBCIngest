using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BBCIngest
{
    public partial class SettingsForm : Form
    {
        private AppSettings appSettings = null;

        public SettingsForm()
        {
            InitializeComponent();
        }

        public AppSettings AppSettings
        {
            get
            {
                return appSettings;
            }

            set
            {
                appSettings = value;
                propertyGrid1.SelectedObject = appSettings;
            }
        }

    }
}
