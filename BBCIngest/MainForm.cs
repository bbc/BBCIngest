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
        private LogDelegate logDelegate;

        public MainForm(AppSettings conf, FetchAndPublish fetcher)
        {
            this.conf = conf;
            this.fetcher = fetcher;
            line1 = new ConcurrentQueue<string>();
            InitializeComponent();
        }

        public void addLogListener(LogDelegate logDelegate)
        {
            this.logDelegate += logDelegate;
        }

        private async void OnLoad(object sender, EventArgs e)
        {
            Version version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            Text = Text + " " + version.ToString();
            Schedule schedule = new Schedule(conf);
            fetcher.listenForTerseMessages(new TerseMessageDelegate(setLine1));
            fetcher.listenForChattyMessages(new ChattyMessageDelegate(setLine1));
            fetcher.listenForEditionStatus(new ShowEditionStatusDelegate(setLine2));
            if (conf.RunInForeground)
            {
                await getLatest(schedule);
                buttonExitOrStart.Text = "Start";
            }
            else
            {
                IScheduleInstaller si = getScheduleInstaller(schedule);
                taskInstalled = si.IsInstalled;
                if(taskInstalled) {
                    buttonRfTS.Text = "Update Task Scheduler";
                }
                else {
                    buttonRfTS.Text = "Install Task";
                }
                buttonExitOrStart.Text = "Exit";
            }
        }

        private IScheduleInstaller getScheduleInstaller(Schedule schedule) {
            if(Environment.OSVersion.Platform == PlatformID.Unix) {
                return new ScheduleInstaller(schedule);
            }
            else {
                Win32ScheduleInstaller si = new Win32ScheduleInstaller(schedule);
                si.addTerseMessageListener(new TerseMessageDelegate(setLine1));
                si.addLogListener(logDelegate);
                return si;
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
            }
            sf.Dispose();
        }

        private void buttonRfTS_Click(object sender, EventArgs e)
        {
            Schedule schedule = new Schedule(conf);
            IScheduleInstaller si = getScheduleInstaller(schedule);
            deleteTask(si);
            createTask(si);
            getLatest(schedule);
        }

        private void buttonRemoveTasks_Click(object sender, EventArgs e)
        {
            Schedule schedule = new Schedule(conf);
            IScheduleInstaller si = getScheduleInstaller(schedule);
            deleteTask(si);
        }

        private async void buttonExitOrStart_Click(object sender, EventArgs e)
        {
            if (conf.RunInForeground)
            {
                if(taskInstalled)
                {
                    Schedule schedule = new Schedule(conf);
                    IScheduleInstaller si = getScheduleInstaller(schedule);
                    deleteTask(si);
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
                    MessageBox.Show("Files will be fetched in the background", "BBC Ingest");
                }
                else
                {
                    MessageBox.Show("No files will be fetched until you update the task scheduler"
                        +"\n or alternatively set to run in the foreground in settings", "BBC Ingest");
                }
                Application.Exit();
            }
        }

        private void createTask(IScheduleInstaller si)
        {
            string progPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string arguments = "once";
            if(conf.SettingsPath != conf.DefaultSettingsPath) {
                arguments = arguments + " \"" + conf.SettingsPath + "\"";
            }
            taskInstalled = si.installTask(progPath, arguments);
            if(taskInstalled)
                si.runTask();
        }

        private void deleteTask(IScheduleInstaller si)
        {
            si.deleteTaskAndTriggers();
            taskInstalled = false;
            setLine1("Tasks removed");
            buttonRfTS.Text = "Install Task";
        }

        private System.Threading.Tasks.Task getLatest(Schedule schedule)
        {
            DateTime? next = schedule.next();
            if (next != null)
            {
                setLine1("Task installed and will next run at " + next.Value);
                //setLine2("Latest is " + fetcher.lastWeHave());
            }
            return fetcher.showLatest();
        }
    }
}
