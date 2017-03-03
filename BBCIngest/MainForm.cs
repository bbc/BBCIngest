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
        private ConcurrentQueue<String> line1;
        FetchAndPublish fetcher;
        private bool taskInstalled = false;

        public MainForm(AppSettings conf)
        {
            this.conf = conf;
            line1 = new ConcurrentQueue<string>();
            InitializeComponent();
            fetcher = new FetchAndPublish(conf);
            fetcher.listenForTerseMessages(new TerseMessageDelegate(setLine1));
            fetcher.listenForChattyMessages(new ChattyMessageDelegate(setLine1));
            fetcher.listenForEditionStatus(new ShowEditionStatusDelegate(setLine2));
        }

        private async void OnLoad(object sender, EventArgs e)
        {
            ScheduleInstaller schedule = new ScheduleInstaller(conf);
            await getLatest(schedule);
            if (conf.RunInForeground)
            {
                buttonExitOrStart.Text = "Start";
            }
            else
            {
                buttonExitOrStart.Text = "Exit";
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            string s;
            if (line1.TryDequeue(out s))
                label1.Text = s;
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
            ScheduleInstaller schedule = new ScheduleInstaller(conf);
            deleteTask(schedule);
            createTask(schedule);
            getLatest(schedule);
        }

        private void buttonRemoveTasks_Click(object sender, EventArgs e)
        {
            ScheduleInstaller schedule = new ScheduleInstaller(conf);
            deleteTask(schedule);
        }

        private async void buttonExitOrStart_Click(object sender, EventArgs e)
        {
            if (conf.RunInForeground)
            {
                if(taskInstalled)
                {
                    deleteTask(new ScheduleInstaller(conf));
                }
                await fetcher.republish();
                while (true)
                {
                    DateTime bc = await fetcher.fetchAndPublish(DateTime.UtcNow);
                    // wait until after broadcast date before trying for next edition
                    await fetcher.waitUntil(bc);
                }
            }
            else
            {
                if (taskInstalled)
                {
                    MessageBox.Show("Files will be fetched in the background");
                    Application.Exit();
                }
                else
                {
                    MessageBox.Show("Update task scheduler to fetch files in the background");
                }
            }
        }

        private void createTask(ScheduleInstaller schedule)
        {
            string path = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            TaskDefinition td = schedule.createTaskDefinition(path);
            if(conf.RunAsService)
            {
                if(schedule.installTaskAsService(td)==false)
                {
                    setLine1("either set RunAsService false in settings or run this program with Admin privileges");
                    taskInstalled = false;
                    return;
                }
            }
            else
            {
                schedule.installUserTask(td);
            }
            schedule.runTask();
            taskInstalled = true;
        }

        private void deleteTask(ScheduleInstaller schedule)
        {
            DateTime? next = schedule.nextRun();
            if (next != null)
            {
                schedule.deleteTaskAndTriggers();
                taskInstalled = false;
                setLine1("Tasks removed");
                buttonRfTS.Text = "Install Task";
            }
        }

        private System.Threading.Tasks.Task getLatest(ScheduleInstaller schedule)
        {
            DateTime? next = schedule.nextRun();
            if (next != null)
            {
                setLine1("Task installed and will next run at " + next.Value);
                setLine2("Latest is " + fetcher.lastWeHave());
                taskInstalled = true;
                buttonRfTS.Text = "Update Task Scheduler";
            }
            else
            {
                buttonRfTS.Text = "Install Task";
            }
            return fetcher.showLatest();
        }

    }
}
