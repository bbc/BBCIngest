using System;
using System.Windows.Forms;

namespace BBCIngest
{
    public partial class MainForm : Form
    {
        private Fetch fetcher = null;

        public MainForm()
        {
            InitializeComponent();
        }

        private async void OnLoad(object sender, EventArgs e)
        {
            fetcher = new Fetch(this);
            await fetcher.main();
        }

        private void sendlog()
        {
            //var url = "mailto:emailnameu@domain.com&attachment=a.txt";
            //System.Diagnostics.Process.Start(url);
        }

        public void setLine1(string s)
        {
            label1.Text = s;
        }

        public void setLine2(string s)
        {
            label2.Text = s;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SettingsForm sf = new SettingsForm();
            sf.AppSettings = fetcher.Conf;
            DialogResult r = sf.ShowDialog(this);
            if(r == DialogResult.OK)
            {
                fetcher.Conf = sf.AppSettings;
                fetcher.Conf.SaveAppSettings();
            }
            sf.Dispose();
        }
    }
}
