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
        private CancellationTokenSource tokenSource;

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
                await Task.Delay(d, tokenSource.Token);
            }
        }

        private async Task<DateTimeOffset?> waitforChange(string file, DateTime prev, DateTime end)
        {
            HttpResponseMessage response = null;
            do
            {
                HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Head, conf.Prefix);
                response = await hc.SendAsync(msg);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    DateTimeOffset? r = response.Content.Headers.LastModified;
                    if (r != null && r > prev)
                    {
                        mainForm.setLine1(
                            "New " + file + " " + response.ReasonPhrase + " at " + DateTime.UtcNow.ToString("HH:mm:ss"));
                        response.Dispose();
                        msg.Dispose();
                        return r;
                    }
                }
                mainForm.setLine1(
                    "Old " + file + " " + response.ReasonPhrase + " at " + DateTime.UtcNow.ToString("HH:mm:ss"));
                response.Dispose();
                msg.Dispose();
                await Task.Delay(10 * 1000);
            }
            while (DateTime.UtcNow < end);
            return null;
        }

        private async Task<DateTimeOffset?> waitfor(DateTime t, DateTime old, DateTime end)
        {
            string file = conf.Basename + "." + conf.Suffix;
            string url = conf.Prefix;
            if(conf.Webdate != "")
            {
                file = conf.webname(t);
                url += file;
            }
            HttpResponseMessage response = null;
            do
            {
                HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Head, url);
                response = await hc.SendAsync(msg);
                msg.Dispose();
                mainForm.setLine1(
                    file + " " + response.ReasonPhrase + " at " + DateTime.UtcNow.ToString("HH:mm:ss"));
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    DateTimeOffset? r = response.Content.Headers.LastModified;
                    if (conf.Webdate != "")
                    {
                        response.Dispose();
                        return r;
                    }
                    else
                    {
                        if (r > old)
                        {
                            response.Dispose();
                            return r;
                        }
                    }
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
            DateTime dt = f.CreationTimeUtc;
            if (conf.Suffix == "mp3")
            {
                TagLib.File tf = TagLib.File.Create(f.FullName);
                string s = tf.Tag.Comment.Replace("UTC", "GMT");
                dt = DateTime.Parse(s);
                tf.Dispose();
            }
            mainForm.setLine2("Latest is " + dt.ToString());
            return dt;
        }

        private void publish(DateTime t)
        {
            FileInfo f = new FileInfo(conf.latest());
            if (f.Exists == false)
            {
                return;
            }
            DateTime dt = latestPublishTime(f);
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

        private async Task<DateTimeOffset?> fetchOld(DateTime t)
        {
            mainForm.setLine1("creating ingest using latest edition");
            DateTime prev = schedule.previous();
            DateTime old = prev.AddMilliseconds(-10);
            DateTimeOffset? lmd = await waitfor(prev, old, DateTime.UtcNow);
            if (lmd != null)
            {
                FileInfo have = new FileInfo(conf.latest());
                // prev will be the nominal time. latestPublishTime should be a few minutes earlier
                if (have.Exists == false || old > latestPublishTime(have))
                {
                    await save(prev);
                }
                publish(prev);
            }
            return lmd;
        }

        private void badMessage(DateTime t, DateTime lmd)
        {
            string logmessage = t.ToString("HH:mm") + " edition was not found, using " + lmd + " edition";
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

        public async Task main()
        {
            tokenSource = new CancellationTokenSource();
            HttpClientHandler httpClientHandler = new HttpClientHandler()
            {
                Proxy = WebRequest.GetSystemWebProxy()
            };
            hc = new HttpClient(httpClientHandler);
            while (true)
            {
                log = new Logging(conf, hc);
                schedule = new Schedule(conf);
                DateTimeOffset? lmd = await fetchOld(schedule.previous());
                FileInfo f = new FileInfo(conf.latest());
                string s = f.FullName;
                DateTime latest;
                if (f.Exists)
                {
                    latest = latestPublishTime(f);
                }
                else
                {
                    mainForm.setLine2("no file yet");
                }
                try
                {
                    while (true)
                    {
                        DateTimeOffset? nlmd = null;
                        DateTime t = schedule.next();
                        // wait until a few minutes before publication
                        await waitnear(t);
                        lmd = await fetchOld(t);
                        nlmd = await waitfor(t, t.AddMinutes(-10), t.AddMinutes(conf.BroadcastMinuteAfter));
                        if (nlmd == null)
                        {
                            badMessage(t, lmd.Value.UtcDateTime);
                        }
                        else
                        {
                            DateTime before = DateTime.UtcNow;
                            await save(t);
                            publish(t);
                            DateTime after = DateTime.UtcNow;
                            goodMessage(t, nlmd.Value.UtcDateTime, before, after);
                        }
                        await Task.Delay(5000); // let the user see the message
                        // wait until after pubdate before trying for next edition
                        int d = (int)t.Subtract(DateTime.UtcNow).TotalMilliseconds;
                        if (d > 0)
                        {
                            mainForm.setLine1("waiting until " + t.ToString("t"));
                            await Task.Delay(d);
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show(ex.Message);
                    log.WriteLine(ex.Message);
                }
                catch (TaskCanceledException ex2)
                {
                    mainForm.setLine1("restarting after config change");
                }
            }
        }
    }
}
