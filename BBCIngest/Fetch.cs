using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Windows.Forms;

namespace BBCIngest
{
    class Fetch
    {
        private AppSettings conf = new AppSettings();
        private Logging log;
        private Schedule schedule;
        private MainForm mainForm;
        private HttpClient hc;

        public Fetch(MainForm mainForm)
        {
            this.mainForm = mainForm;
        }

        public AppSettings Conf
        {
            get
            {
                return conf;
            }

            set
            {
                conf = value;
            }
        }

        private string name(DateTime t, string fmt)
        {
            return conf.Basename + t.ToString(fmt) + "." + conf.Suffix;
        }

        private string latest()
        {
            return conf.Archive + conf.Basename + "." + conf.Suffix;
        }

        private async Task waitnear(DateTime t)
        {
            DateTime pub = t.AddMinutes(0 - conf.MinutesBefore);
            int d = (int)pub.Subtract(DateTime.UtcNow).TotalMilliseconds;
            if (d > 0)
            {
                mainForm.setLine1("waiting until " + pub.ToString("t"));
                await Task.Delay(d);
            }
        }

        private async Task<string> waitfor(DateTime t, DateTime end)
        {
            string file = name(t, conf.Webdate);
            string url = conf.Prefix + file;
            HttpResponseMessage response = null;
            do
            {
                HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Head, url);
                response = await hc.SendAsync(msg);
                mainForm.setLine1(
                    file + " " + response.ReasonPhrase + " at " + DateTime.UtcNow.ToString("HH:mm:ss"));
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string r = response.Content.Headers.LastModified.ToString();
                    response.Dispose();
                    return r;
                }
                else
                {
                    await Task.Delay(10 * 1000);
                }
            }
            while (DateTime.UtcNow < end);
            response.Dispose();
            return null;
        }

        private async Task save(DateTime t)
        {
            string tmpname = conf.Archive + conf.Basename + ".tmp";
            Stream stream = await hc.GetStreamAsync(conf.Prefix + name(t, conf.Webdate));
            Stream ds = System.IO.File.Open(tmpname, FileMode.OpenOrCreate);
            await stream.CopyToAsync(ds);
            ds.Dispose();
            stream.Dispose();
            FileInfo f = new FileInfo(tmpname);
            string savename = latest();
            System.IO.File.Delete(savename);
            f.MoveTo(savename);
        }

        private DateTime publish(DateTime t)
        {
            string fn = latest();
            FileInfo f = new FileInfo(fn);
            if (f.Exists == false)
            {
                return DateTime.UtcNow.AddDays(-1); // make it old as can't make it null
            }
            DateTime dt = f.LastWriteTimeUtc;
            if (conf.Prefix == "mp3")
            {
                TagLib.File tf = TagLib.File.Create(fn);
                string s = tf.Tag.Comment.Replace("UTC", "GMT");
                dt = DateTime.Parse(s);
                tf.Dispose();
            }
            mainForm.setLine2("Latest is " + dt.ToString());
            string discname = name(t, conf.Discdate);
            if (conf.UseLocaltime)
            {
                discname = name(t.ToLocalTime(), conf.Discdate);
            }
            f.CopyTo(conf.Publish + discname, true);
            return dt;
        }

        private async Task fetch(DateTime t)
        {
            mainForm.setLine1("creating ingest using latest edition");
            DateTime published = publish(t);
            DateTime prev = schedule.previous();
            string lmd = null;
            if (published < prev) // there might be a newer one
            {
                lmd = await waitfor(prev, DateTime.UtcNow);
                if (lmd != null)
                {
                    await save(prev);
                    publish(prev);
                }
            }
            string nlmd = await waitfor(t, t.AddMinutes(conf.BroadcastMinuteAfter));
            DateTime before = DateTime.UtcNow;
            if (nlmd != null)
            {
                await save(t);
                publish(t);
            }
            if (nlmd == null)
            {
                string logmessage = t.ToString("HH:mm") + " edition was not found, using " + lmd + " edition";
                mainForm.setLine1(logmessage);
                log.WriteLine(logmessage);
                await Task.Delay(5000); // let the user see this message
            }
            else
            {
                DateTime after = DateTime.UtcNow;
                string logmessage = t.ToString("HH:mm") + " edition"
                    + " published at " + nlmd + " and downloaded at " + before.ToString("HH:mm:ss")
                    + " in " + Math.Round(after.Subtract(before).TotalSeconds, 2) + "s"
                    + " by " + conf.Station + " in " + conf.City;
                log.WriteLine(logmessage);
                // wait until after pubdate before trying for next edition
                int d = (int)t.Subtract(after).TotalMilliseconds;
                if (d > 0)
                {
                    mainForm.setLine1("waiting until " + t.ToString("t"));
                    await Task.Delay(d);
                }
            }
        }

        public async Task main()
        {
            // TODO button to allow log to be emailed and cleared.
            conf.LoadAppSettings();
            Directory.CreateDirectory(conf.Publish);
            Directory.CreateDirectory(conf.Archive);
            Directory.CreateDirectory(conf.Logfolder);
            HttpClientHandler httpClientHandler = new HttpClientHandler()
            {
                Proxy = WebRequest.GetSystemWebProxy()
            };
            hc = new HttpClient(httpClientHandler);
            log = new Logging(conf.Logfolder + conf.Basename + ".log");
            schedule = new Schedule(conf);
            try
            {
                while (true)
                {
                    DateTime t = schedule.next();
                    // wait until a few minutes before publication
                    await waitnear(t);
                    await fetch(t);
                }
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show(ex.Message);
                log.WriteLine(ex.Message);
            }
        }
    }
}
