using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
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
                int avw = appSettings.ValueWidth();
                int alw = appSettings.LabelWidth();
                propertyGrid1.Width = 7 * (alw + avw);
            }
        }

    }
}
