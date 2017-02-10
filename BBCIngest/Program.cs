using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Ingest;

namespace BBCIngest
{
    static class Program
    {
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
            conf.LoadAppSettings();
            conf.SaveAppSettings();
            Directory.CreateDirectory(conf.Publish);
            Directory.CreateDirectory(conf.Archive);
            Directory.CreateDirectory(conf.Logfolder);
            if (arg.Equals(""))
            {
                Application.Run(new MainForm(conf));
            }
            else if(arg.Equals("install"))
            {
                foreach (var row in File.ReadAllLines("init.properties"))
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
                conf.SaveAppSettings();
                Application.Run(new MainForm(conf));
            }
            else
            {
                MainTask(conf).Wait();
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
