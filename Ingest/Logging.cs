using System;
using System.IO;
using System.Net.Http;
using System.Text;

namespace Ingest
{
    public delegate void LogDelegate(string s);

    public class Logging
    {
        private event LogDelegate logevent;
        private AppSettings conf;
        private HttpClient hc;

        public Logging(AppSettings conf, HttpClient hc)
        {
            this.conf = conf;
            this.hc = hc;
            this.logevent += new LogDelegate(write);
            this.logevent += new LogDelegate(post);
        }

        public void WriteLine(string logmessage)
        {
            logevent(logmessage);
        }

        internal void write(string logmessage)
        {
            DateTime dt = DateTime.UtcNow;
            StreamWriter log = System.IO.File.AppendText(conf.Logfolder + "bbcingest.log");
            log.WriteLine(dt.ToString() + " "+ logmessage + " by " + conf.Station + " in " + conf.City);
            log.Dispose();
        }

        internal void post(string logmessage)
        {
            DateTime dt = DateTime.UtcNow;
            if (conf.PostLogs && conf.LogUrl != "" && hc != null)
            {
                string f = "\"date\": \"{0:yyyy-MM-ddTHH:mm:ssZ}\", \"message\": \"{1}\", \"city\": \"{2}\", \"station\": \"{3}\"";
                string jsonObject = "[{" + string.Format(f, dt, logmessage, conf.City, conf.Station) + "}]";
                var content = new StringContent(jsonObject, Encoding.UTF8, "application/json");
                hc.PostAsync(conf.LogUrl, content);
            }
        }
    }
}
