using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;

namespace Ingest
{
    public interface IFetchSettings
    {
        string Archive { get; set; }
        string Basename { get; set; }
        string Prefix { get; set; }
        string Webdate { get; set; }
        string Suffix { get; set; }
        string dateTimeToString(string format, DateTime epoch);
        string PublishName { get; set; }
        string PublishFormat { get; set; }
        int RetryIntervalSeconds { get; set; }
        int MaxAgeMinutes { get; set; }
    }

    public class Fetch
    {
        private event TerseMessageDelegate terseMessage;
        private event ChattyMessageDelegate chattyMessage;
        private event ShowEditionStatusDelegate showEditionStatus;
        private event LogDelegate logger;
        private IFetchSettings conf;
        private HttpClient hc;

        public Fetch(IFetchSettings conf, HttpClient hc)
        {
            this.conf = conf;
            this.hc = hc;
        }

        public void addTerseMessageListener(TerseMessageDelegate fm)
        {
            this.terseMessage += fm;
        }

        public void addChattyMessageListener(ChattyMessageDelegate fm)
        {
            this.chattyMessage += fm;
        }

        public void addEditionListener(ShowEditionStatusDelegate ne)
        {
            this.showEditionStatus += ne;
        }

        public void addLogListener(LogDelegate logDelegate)
        {
            this.logger += logDelegate;
        }

        public string webname(DateTime t)
        {
            string n = conf.Basename;
            if (conf.Webdate != "")
                n += conf.dateTimeToString(conf.Webdate, t);
            if (conf.Suffix != "")
                n += "." + conf.Suffix;
            return n;
        }

        private string url(DateTime epoch)
        {
            return conf.Prefix + webname(epoch);
        }

        public string lastWeHave()
        {
            return conf.Archive + conf.PublishName + "." + conf.PublishFormat;
        }

        /*
         * Note - this assumes files are uploaded near to the epoch
         * For example BBC Minute is published 2-4 minutes before
         * but in extremes up to 14 minutes after.
         * 5 minute news bulletin is live and published about 1 minute after the end
         * so around 6 minutes after the epoch
         * TODO - think about early publishing - not intractable if filenames are unique to the edition
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
            DateTime dt = r.Value.DateTime;
            if (dt < epoch.AddMinutes(-conf.MaxAgeMinutes))
                return null; //remote file is too old - we don't want it
            chattyMessage(dt + " edition is available");
            return dt;
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
                chattyMessage("Waiting for " + t.ToString("HH:mm") + " edition at " + DateTime.UtcNow.ToString("HH:mm:ss"));
                await Task.Delay(1000 * conf.RetryIntervalSeconds);
            }
            while (DateTime.UtcNow < end);
            return null;
        }

        public async Task save(DateTime t)
        {
            DateTime before = DateTime.UtcNow;
            string tmpname = conf.Archive + "bbcingest.tmp";
            var u = url(t);
            
            HttpResponseMessage m = await hc.GetAsync(u);
            Stream ds = System.IO.File.Open(tmpname, FileMode.OpenOrCreate);
            await m.Content.CopyToAsync(ds);    
            ds.Dispose();
            FileInfo f = new FileInfo(tmpname);
            string savename = lastWeHave();
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

            showEditionStatus("Latest is " + pt);
            string message = t.ToString("HH:mm") + " edition"
                + " published at " + pt
                + " and downloaded at " + before.ToString("HH:mm:ss")
                + " in " + Math.Round(after.Subtract(before).TotalSeconds, 2) + "s";
            logger(message);
            terseMessage("Fetched " + pt + " edition");
        }

        public DateTime latestPublishTime(FileInfo f)
        {
            DateTime dt = f.LastWriteTimeUtc;
            if (f.Extension == ".mp3")
            {
                try
                {   //added to catch exceptions when file does not contain any ID3 tags
                    //use the default lastwritetimeutc instead
                    TagLib.File tf = TagLib.File.Create(f.FullName);
                    string s = tf.Tag.Comment;
                    if (s != null)
                    {
                        dt = DateTime.Parse(s.Replace("UTC", "GMT"));
                    }
                    tf.Dispose();
                }
                catch { }
            }
            return dt;
        }

        public async Task<bool> shouldRefetch(DateTime epoch)
        {
            FileInfo f = new FileInfo(lastWeHave());
            DateTime? newest = await editionAvailable(epoch);
            if (newest == null)
            {
                if (f.Exists)
                {
                    showEditionStatus("Latest is " + latestPublishTime(f));
                }
                else
                {
                    showEditionStatus("No file yet");
                }
                return false; // might want to but internet might not be available
            }
            else
            {
                if (f.Exists)
                {
                    if (newest.Value > f.LastWriteTimeUtc)
                    {
                        return true;
                    }
                    else
                    {
                        showEditionStatus("Latest is " + latestPublishTime(f));
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
        }

        public async Task reFetchIfNeeded(DateTime epoch)
        {
            bool needed = await shouldRefetch(epoch);
            if (needed)
                await save(epoch);
        }
    }
}
