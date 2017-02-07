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
