using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Windows.Forms;
using System.Threading;

namespace BBCIngest
{
    class Fetch
    {
        private AppSettings conf;
        private Logging log;
        private Schedule schedule;
        private MainForm mainForm;
        private HttpClient hc;

        public Fetch(MainForm mainForm, AppSettings conf)
        {
            this.mainForm = mainForm;
            this.conf = conf;
        }

        internal void ChangeConfig(AppSettings conf)
        {
            this.conf = conf;
            log.WriteLine("new config");
            //tokenSource.Cancel();
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

        /*
         * Note - this assumes files are uploaded near to the epoch
         * For example BBC Minute is published 2-4 minutes before
         * but in extremes up to 14 minutes after.
         * 5 minute news bulletin is live and published about 1 minute after the end
         * so around 6 minutes after the epoch
         * TODO - think about early publishing - not untractable if filenames are unique to the edition
         */
        private async Task<DateTime?> editionAvailable(DateTime epoch)
        {
            string url = conf.Prefix;
            bool staticUrl = (conf.Webdate == "");
            if (!staticUrl)
            {
                url += conf.webname(epoch);
            }
            HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Head, url);
            HttpResponseMessage response = await hc.SendAsync(msg);
            msg.Dispose();
            DateTimeOffset? r = response.Content.Headers.LastModified;
            response.Dispose();
            if (r == null)
                return null;
            if (r < epoch.AddMinutes(-10))
                return null;
            return r.Value.DateTime;
        }

        private async Task<DateTime?> waitfor(DateTime t, DateTime end)
        {
            DateTime? lmd = null;
            do
            {
                lmd = await editionAvailable(t);
                if (lmd != null)
                {
                    return lmd;
                }
                mainForm.setLine1("Waiting for " + t.ToString("HH:mm") + " edition at " + DateTime.UtcNow.ToString("HH:mm:ss"));
                await Task.Delay(10 * 1000);
            }
            while (DateTime.UtcNow < end);
            return null;
        }

        private async Task save(DateTime t)
        {
            string tmpname = conf.Archive + conf.Basename + ".tmp";
            string url = conf.Prefix;
            if (conf.Webdate != "")
            {
                url += conf.webname(t);
            }
            HttpResponseMessage m = await hc.GetAsync(url);
            Stream ds = System.IO.File.Open(tmpname, FileMode.OpenOrCreate);
            await m.Content.CopyToAsync(ds);
            ds.Dispose();
            FileInfo f = new FileInfo(tmpname);
            string savename = conf.latest();
            System.IO.File.Delete(savename);
            f.MoveTo(savename);
            if (m.Content.Headers.LastModified != null)
            {
                DateTime lm = m.Content.Headers.LastModified.Value.UtcDateTime;
                f.CreationTimeUtc = lm;
                f.LastWriteTimeUtc = lm;
            }
        }

        private DateTime latestPublishTime(FileInfo f)
        {
            DateTime dt = f.LastWriteTimeUtc;
            if (conf.Suffix == "mp3")
            {
                TagLib.File tf = TagLib.File.Create(f.FullName);
                string s = tf.Tag.Comment.Replace("UTC", "GMT");
                dt = DateTime.Parse(s);
                tf.Dispose();
            }
            return dt;
        }

        private void publish(DateTime t)
        {
            FileInfo f = new FileInfo(conf.latest());
            if (f.Exists == false)
            {
                return;
            }
            mainForm.setLine2("Latest is " + latestPublishTime(f));
            string savename = conf.Publish + conf.discname(t);
            if (conf.SafePublishing)
            {
                string tempname = conf.Publish + conf.Basename + ".tmp";
                try
                {
                    FileInfo pf = f.CopyTo(tempname, true);
                    // if no exception we are OK to overwrite
                    System.IO.File.Delete(savename);
                    pf.MoveTo(savename);
                }
                catch
                {
                    mainForm.setLine1("error writing to " + conf.Publish + " folder");
                }
            }
            else
            {
                f.CopyTo(savename, true);
            }
        }

        private void badMessage(DateTime t)
        {
            string logmessage = "";
            FileInfo f = new FileInfo(conf.latest());
            if (f.Exists)
            {
                DateTime lmd = latestPublishTime(f);
                logmessage = t.ToString("HH:mm") + " edition was not found, using "
                    + lmd.ToString("HH:mm") + " edition";
            }
            else
            {
                logmessage = "no usable file";
            }
            mainForm.setLine1(logmessage);
            log.WriteLine(logmessage);
        }

        private void goodMessage(DateTime t, DateTime lmd, DateTime before, DateTime after)
        {
            string logmessage = t.ToString("HH:mm") + " edition"
                + " published at " + lmd + " and downloaded at " + before.ToString("HH:mm:ss")
                + " in " + Math.Round(after.Subtract(before).TotalSeconds, 2) + "s";
            log.WriteLine(logmessage);
        }

        public async Task republish()
        {
            mainForm.setLine1("creating ingest using latest edition");
            DateTime prev = schedule.previous();
            DateTime? lmd = await editionAvailable(prev);
            if (lmd == null)
            {
                mainForm.setLine2("no file yet");
            }
            else
            {
                FileInfo f = new FileInfo(conf.latest());
                if (f.Exists == false || f.LastWriteTimeUtc < lmd)
                {
                    await save(prev);
                }
                publish(prev);
            }
        }

        public async Task main()
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler()
            {
                Proxy = WebRequest.GetSystemWebProxy()
            };
            hc = new HttpClient(httpClientHandler);
            log = new Logging(conf, hc);
            schedule = new Schedule(conf);
            await republish();
            await Task.Delay(2000); // let the user see the message
            while (true)
            {
                try
                {
                    DateTime? lmd = null;
                    DateTime t = schedule.next();
                    // wait until a few minutes before publication
                    await waitnear(t);
                    await republish();
                    lmd = await waitfor(t, t.AddMinutes(conf.BroadcastMinuteAfter));
                    if (lmd == null)
                    {
                        badMessage(t);
                    }
                    else
                    {
                        DateTime before = DateTime.UtcNow;
                        await save(t);
                        publish(t);
                        DateTime after = DateTime.UtcNow;
                        goodMessage(t, lmd.Value, before, after);
                    }
                    await Task.Delay(2000); // let the user see the message
                    // wait until after pubdate before trying for next edition
                    int d = (int)t.Subtract(DateTime.UtcNow).TotalMilliseconds;
                    if (d > 0)
                    {
                        mainForm.setLine1("waiting until " + t.ToString("t"));
                        await Task.Delay(d);
                    }
                }
                catch (HttpRequestException ex)
                {
                    mainForm.setLine1(ex.Message);
                    log.WriteLine(ex.Message);
                }
            }
        }
    }
}
