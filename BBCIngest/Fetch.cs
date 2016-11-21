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

        private async Task<DateTimeOffset?> waitfor(DateTime t, DateTime end)
        {
            string file = conf.webname(t);
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
                    DateTimeOffset? r = response.Content.Headers.LastModified;
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

        private async Task save(DateTime t, DateTimeOffset? cdto)
        {
            string tmpname = conf.Archive + conf.Basename + ".tmp";
            Stream stream = await hc.GetStreamAsync(conf.Prefix + conf.webname(t));
            Stream ds = System.IO.File.Open(tmpname, FileMode.OpenOrCreate);
            await stream.CopyToAsync(ds);
            ds.Dispose();
            stream.Dispose();
            FileInfo f = new FileInfo(tmpname);
            string savename = conf.latest();
            System.IO.File.Delete(savename);
            f.MoveTo(savename);
            if(cdto != null)
            {
                f.CreationTimeUtc = cdto.Value.UtcDateTime;
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
            DateTimeOffset? lmd = await waitfor(prev, DateTime.UtcNow);
            if (lmd != null)
            {
                FileInfo old = new FileInfo(conf.latest());
                // prev will be the nominal time. latestPublishTime should be a few minutes earlier
                if (old.Exists == false || prev.AddMinutes(-10) > latestPublishTime(old))
                {
                    await save(prev, lmd);
                }
                publish(prev);
            }
            return lmd;
        }

        private async Task fetch(DateTime t)
        {
            DateTimeOffset? lmd = await fetchOld(t);
            DateTimeOffset? nlmd = await waitfor(t, t.AddMinutes(conf.BroadcastMinuteAfter));
            DateTime before = DateTime.UtcNow;
            if (nlmd != null)
            {
                await save(t, nlmd);
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
                    + " in " + Math.Round(after.Subtract(before).TotalSeconds, 2) + "s";                    
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
                if (f.Exists)
                {
                    latestPublishTime(f);
                }
                else
                {
                    mainForm.setLine2("no file yet");
                }
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
                catch (TaskCanceledException ex2)
                {
                    mainForm.setLine1("restarting after config change");
                }
            }
        }
    }
}
