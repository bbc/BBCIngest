using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Ingest;

namespace BBCIngest
{
    static class Program
    {
        private static string init_file = "init.properties";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(String[] args)
        {
            String arg = "";
            if (args.Length > 0)
            {
                arg = args[0];
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AppSettings conf = new AppSettings();
            if (args.Length == 2)
            {
                conf.SettingsPath = args[1];
            }
            conf.LoadAppSettings();
            Directory.CreateDirectory(conf.PublishFolder);
            Directory.CreateDirectory(conf.Archive);
            Directory.CreateDirectory(conf.Logfolder);
            //MessageBox.Show(arg, "BBCIngest", MessageBoxButtons.OK);
            if (arg.Equals("install"))
            {
                if (File.Exists(init_file))
                {
                    foreach (var row in File.ReadAllLines(init_file))
                    {
                        string[] s = row.Split('=');
                        if (s[0].Equals("postLogs"))
                            conf.PostLogs = s[1].Equals("1");
                        if (s[0].Equals("city"))
                            conf.City = s[1];
                        if (s[0].Equals("station"))
                            conf.Station = s[1];
                        if (s[0].Equals("logUrl"))
                            conf.LogUrl = s[1];
                    }
                }
                conf.SaveAppSettings();
            }
            else if (arg.Equals("uninstall"))
            {
                Schedule schedule = new Schedule(conf);
                IScheduleInstaller si;
                if(Environment.OSVersion.Platform == PlatformID.Unix) {
                  si = new ScheduleInstaller(schedule);
                }
                else {
                  si = new Win32ScheduleInstaller(schedule);
                }
                si.deleteTaskAndTriggers();
                //MessageBox.Show("Scheduled Tasks Removed", "BBCIngest", MessageBoxButtons.OK);
            }
            else if (arg.Equals("once"))
            {
                MainTask(conf).Wait();
            }
            else
            {
                Application.Run(new MainForm(conf));
            }
        }
       
        static async Task MainTask(AppSettings conf)
        {
            FetchAndPublish fetcher = new FetchAndPublish(conf);
            TrayNotify notify = new TrayNotify(fetcher);
            await fetcher.republish();
            DateTime bc = await fetcher.fetchAndPublish(DateTime.UtcNow);
        }
    }
}
