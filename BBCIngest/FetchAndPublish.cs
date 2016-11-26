using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace BBCIngest
{
    public delegate void NewEditionDelegate(string s);
    public delegate void FetchMessageDelegate(string s);

    class FetchAndPublish
    {
        private event FetchMessageDelegate fetchMessage;

        private AppSettings conf;
        private Logging log;
        private Schedule schedule;
        private HttpClient hc;
        Fetch fetcher;
        Publish publisher;

        public FetchAndPublish(AppSettings conf)
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler()
            {
                Proxy = WebRequest.GetSystemWebProxy()
            };
            hc = new HttpClient(httpClientHandler);
            this.conf = conf;
            this.fetcher = new Fetch(conf, hc);
            this.publisher = new Publish(conf);
            log = new Logging(conf, hc);
            schedule = new Schedule(conf);
            fetcher.addLogListener(new LogDelegate(log.WriteLine));
        }

        public void addMessageListener(FetchMessageDelegate fm)
        {
            this.fetchMessage += fm;
            publisher.addMessageListener(fm);
            fetcher.addMessageListener(fm);
        }

        public void addEditionListener(NewEditionDelegate ne)
        {
            fetcher.addEditionListener(ne);
        }

        internal void ChangeConfig(AppSettings conf)
        {
            this.conf = conf;
            log.WriteLine("new config");
        }

        private async Task waitUntil(DateTime t)
        {
            int d = (int)t.Subtract(DateTime.UtcNow).TotalMilliseconds;
            if (d > 0)
            {
                fetchMessage("waiting until " + t.ToString("t"));
                await Task.Delay(d);
            }
        }

        public async Task main()
        {
            DateTime current = schedule.current(DateTime.UtcNow);
            DateTime next = schedule.next(DateTime.UtcNow);
            // on startup make sure we have the latest edition
            await fetcher.reFetchIfNeeded(current);
            // (re-)publish it
            publisher.publish(fetcher.latest(), current);
            await Task.Delay(4000); // let the user see the message
            while (true)
            {
                try
                {
                    DateTime now = DateTime.UtcNow;
                    DateTime? lmd = null;
                    DateTime t = schedule.current(now);
                    DateTime bc = t.AddMinutes(conf.BroadcastMinuteAfter);
                    if (now < bc) // check if we have time to publish a late file
                    {
                        await fetcher.reFetchIfNeeded(t);
                    }
                    else  // no we don't
                    {
                        t = schedule.next(now);
                        // wait until a few minutes before publication
                        await waitUntil(t.AddMinutes(0 - conf.MinutesBefore));
                        await fetcher.reFetchIfNeeded(t);
                        bc = t.AddMinutes(conf.BroadcastMinuteAfter);
                    }
                    // publish most recent as the next edition in case we can't get the next one
                    publisher.publish(fetcher.latest(), t);
                    lmd = await fetcher.waitfor(t, bc);
                    if (lmd == null)
                    {
                        badMessage(t);
                    }
                    else
                    {
                        DateTime before = DateTime.UtcNow;
                        await fetcher.save(t);
                        publisher.publish(fetcher.latest(), t);
                        fetchMessage(t.ToString("HH:mm") + " edition published at " + lmd);
                    }
                    await Task.Delay(4000); // let the user see the message
                    // wait until after broadcast date before trying for next edition
                    await waitUntil(bc);
                }
                catch (Exception ex)
                {
                    fetchMessage(ex.Message);
                    log.WriteLine(ex.Message);
                    // best delete the current file so we will fetch another
                    FileInfo f = new FileInfo(fetcher.latest());
                    f.Delete();
                }
            }
        }

        private void badMessage(DateTime t)
        {
            string message = "";
            FileInfo f = new FileInfo(fetcher.latest());
            if (f.Exists)
            {
                DateTime lmd = fetcher.latestPublishTime(f);
                message = t.ToString("HH:mm") + " edition was not found, using "
                    + lmd.ToString("HH:mm") + " edition";
            }
            else
            {
                message = "no usable file";
            }
            fetchMessage(message);
            log.WriteLine(message);
        }
    }
}
