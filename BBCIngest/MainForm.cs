using Microsoft.Win32.TaskScheduler;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace BBCIngest
{
    public partial class MainForm : Form
    {
        private FetchAndPublish fetcher = null;
        private AppSettings conf;
        private Schedule schedule = null;

        public MainForm(AppSettings conf, FetchAndPublish fetcher)
        {
            this.conf = conf;
            this.fetcher = fetcher;
            InitializeComponent();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            schedule = new Schedule(conf);
            fetcher.listenForTerseMessages(new TerseMessageDelegate(setLine1));
            fetcher.listenForChattyMessages(new ChattyMessageDelegate(setLine1));
            fetcher.listenForEditionStatus(new ShowEditionStatusDelegate(setLine2));
        }

        public void setLine1(string s)
        {
            label1.Text = s;
            Thread.Sleep(1000); // let the user see the message
        }

        public void setLine2(string s)
        {
            label2.Text = s;
        }

        private void buttonSettings_Click(object sender, EventArgs e)
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

        private void buttonRfTS_Click(object sender, EventArgs e)
        {
            string path = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            schedule.deleteTaskAndTriggers();
            schedule.createTaskAndTriggers(path);
            schedule.runTask();
        }

        private async void buttonStart_Click(object sender, EventArgs e)
        {
            await fetcher.republish();
            while(true)
            {
                DateTime bc = await fetcher.fetchAndPublish(DateTime.UtcNow);
                // wait until after broadcast date before trying for next edition
                await fetcher.waitUntil(bc);
            }            
        }
    }
}
