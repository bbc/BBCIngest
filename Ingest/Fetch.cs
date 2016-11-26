using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;

namespace BBCIngest
{
    public interface IFetchSettings
    {
        string Archive { get; set; }
        string Basename { get; set; }
        string Prefix { get; set; }
        string Webdate { get; set; }
        string Suffix { get; set; }
        string dateTimeToString(string format, DateTime epoch);
    }

    class Fetch
    {
        private event FetchMessageDelegate fetchMessage;
        private event NewEditionDelegate newEdition;
        private event LogDelegate logger;
        private IFetchSettings conf;
        private HttpClient hc;

        public Fetch(IFetchSettings conf, HttpClient hc)
        {
            this.conf = conf;
            this.hc = hc;
        }

        public void addMessageListener(FetchMessageDelegate fm)
        {
            this.fetchMessage += fm;
        }

        public void addEditionListener(NewEditionDelegate ne)
        {
            this.newEdition += ne;
        }

        public void addLogListener(LogDelegate logDelegate)
        {
            this.logger += logDelegate;
        }

        public string webname(DateTime t)
        {
            return conf.Basename + conf.dateTimeToString(conf.Webdate, t) + "." + conf.Suffix;
        }

        private string url(DateTime epoch)
        {
            return conf.Prefix + webname(epoch);
        }

        public string latest()
        {
            return conf.Archive + conf.Basename + "." + conf.Suffix;
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
            HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Head, url(epoch));
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

        public async Task<DateTime?> waitfor(DateTime t, DateTime end)
        {
            DateTime? lmd = null;
            do
            {
                lmd = await editionAvailable(t);
                if (lmd != null)
                {
                    return lmd;
                }
                fetchMessage("Waiting for " + t.ToString("HH:mm") + " edition at " + DateTime.UtcNow.ToString("HH:mm:ss"));
                await Task.Delay(10 * 1000);
            }
            while (DateTime.UtcNow < end);
            return null;
        }

        public async Task save(DateTime t)
        {
            DateTime before = DateTime.UtcNow;
            string tmpname = conf.Archive + conf.Basename + ".tmp";
            HttpResponseMessage m = await hc.GetAsync(url(t));
            Stream ds = System.IO.File.Open(tmpname, FileMode.OpenOrCreate);
            await m.Content.CopyToAsync(ds);
            ds.Dispose();
            FileInfo f = new FileInfo(tmpname);
            string savename = latest();
            System.IO.File.Delete(savename);
            f.MoveTo(savename);
            DateTime after = DateTime.UtcNow;
            if (m.Content.Headers.LastModified != null)
            {
                DateTime lm = m.Content.Headers.LastModified.Value.UtcDateTime;
                f.CreationTimeUtc = lm;
                f.LastWriteTimeUtc = lm;
            }
            DateTime pt = latestPublishTime(f);

            newEdition("Latest is " + pt);
            string message = t.ToString("HH:mm") + " edition"
                + " published at " + pt
                + " and downloaded at " + before.ToString("HH:mm:ss")
                + " in " + Math.Round(after.Subtract(before).TotalSeconds, 2) + "s";
            logger(message);
        }

        public DateTime latestPublishTime(FileInfo f)
        {
            DateTime dt = f.LastWriteTimeUtc;
            if (f.Extension == ".mp3")
            {
                TagLib.File tf = TagLib.File.Create(f.FullName);
                string s = tf.Tag.Comment;
                if (s != null) {
                    dt = DateTime.Parse(s.Replace("UTC", "GMT"));
                }
                tf.Dispose();
            }
            return dt;
        }

        public async Task reFetchIfNeeded(DateTime epoch)
        {
            DateTime? lmd = await editionAvailable(epoch);
            fetchMessage("creating ingest using latest edition");
            FileInfo f = new FileInfo(latest());
            if(f.Exists)
            {
                if((lmd != null) && (lmd.Value <= f.LastWriteTimeUtc))
                {
                    await save(epoch); // newer file available
                }
                else
                {
                    newEdition("Latest is " + latestPublishTime(f));
                }
            }
            else
            {
                if (lmd == null)
                {
                    newEdition("no file yet");
                }
                else
                {
                    await save(epoch);
                }
            }
        }
    }
}
