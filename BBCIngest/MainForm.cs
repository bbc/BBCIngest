using System;
using System.IO;
using System.Windows.Forms;

namespace BBCIngest
{
    public partial class MainForm : Form
    {
        private FetchAndPublish fetcher = null;
        private AppSettings conf;

        public MainForm()
        {
            InitializeComponent();
        }

        private async void OnLoad(object sender, EventArgs e)
        {
            conf = new AppSettings();
            conf.LoadAppSettings();
            Directory.CreateDirectory(conf.Publish);
            Directory.CreateDirectory(conf.Archive);
            Directory.CreateDirectory(conf.Logfolder);
            fetcher = new FetchAndPublish(conf);
            fetcher.addMessageListener(new FetchMessageDelegate(setLine1));
            fetcher.addEditionListener(new NewEditionDelegate(setLine2));
            await fetcher.main();
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
            sf.AppSettings = conf.ShallowCopy();
            DialogResult r = sf.ShowDialog(this);
            if(r == DialogResult.OK)
            {
                conf = sf.AppSettings;
                conf.SaveAppSettings();
                fetcher.ChangeConfig(conf);
            }
            sf.Dispose();
        }
    }
}
