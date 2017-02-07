using Microsoft.Win32.TaskScheduler;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Ingest;
using System.Collections.Concurrent;

namespace BBCIngest
{
    public partial class MainForm : Form
    {
        private AppSettings conf;
        private ConcurrentQueue<String> line1 = new ConcurrentQueue<string>();

        public MainForm(AppSettings conf)
        {
            this.conf = conf;
            InitializeComponent();
        }

        private void OnLoad(object sender, EventArgs e)
        {
        }

        public void setLine1(string s)
        {
            line1.Enqueue(s);
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
                Logging log = new Ingest.Logging(conf, null);
                log.WriteLine("new config");
            }
            sf.Dispose();
        }

        private void buttonRfTS_Click(object sender, EventArgs e)
        {
            string path = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            ScheduleInstaller schedule = new ScheduleInstaller(conf);
            schedule.deleteTaskAndTriggers();
            schedule.createTaskAndTriggers(path);
            schedule.runTask();
        }

        private async void buttonStart_Click(object sender, EventArgs e)
        {
            FetchAndPublish fetcher = new FetchAndPublish(conf);
            fetcher.listenForTerseMessages(new TerseMessageDelegate(setLine1));
            fetcher.listenForChattyMessages(new ChattyMessageDelegate(setLine1));
            fetcher.listenForEditionStatus(new ShowEditionStatusDelegate(setLine2));
            await fetcher.republish();
            while(true)
            {
                DateTime bc = await fetcher.fetchAndPublish(DateTime.UtcNow);
                // wait until after broadcast date before trying for next edition
                await fetcher.waitUntil(bc);
            }            
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            string s;
            if(line1.TryDequeue(out s))
                label1.Text = s;
        }
    }
}
