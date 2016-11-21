using System;
using System.IO;
using System.Net.Http;
using System.Text;

namespace BBCIngest
{
    class Logging
    {
        private AppSettings conf;
        private HttpClient hc;

        public Logging(AppSettings conf, HttpClient hc)
        {
            this.conf = conf;
            this.hc = hc;
        }

        internal void WriteLine(string logmessage)
        {
            DateTime dt = DateTime.UtcNow;
            StreamWriter log = System.IO.File.AppendText(conf.Logfolder + conf.Basename + ".log");
            log.WriteLine(dt.ToString() + " "+ logmessage + " by " + conf.Station + " in " + conf.City);
            log.Dispose();
            if (conf.PostLogs && conf.LogUrl != "")
            {
                string f = "\"date\": \"{0:yyyy-MM-ddTHH:mm:ssZ}\", \"message\": \"{1}\", \"city\": \"{2}\", \"station\": \"{3}\"";
                string jsonObject = "[{"+string.Format(f, dt, logmessage, conf.City, conf.Station)+"}]";
                var content = new StringContent(jsonObject, Encoding.UTF8, "application/json");
                hc.PostAsync(conf.LogUrl, content);
            }
        }
    }
}
