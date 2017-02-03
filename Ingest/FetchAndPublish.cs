using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace BBCIngest
{
    public delegate void NewEditionDelegate(string s);
    public delegate void TerseMessageDelegate(string s);
    public delegate void ChattyMessageDelegate(string s);

    public class FetchAndPublish
    {
        private event TerseMessageDelegate terseMessage;

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

        public void addTerseMessageListener(TerseMessageDelegate m)
        {
            this.terseMessage += m;
            publisher.addTerseMessageListener(m);
            fetcher.addTerseMessageListener(m);
            conf.addTerseMessageListener(m); // so exceptions in the AppSettings can be reported
        }

        public void addChattyMessageListener(ChattyMessageDelegate m)
        {
            fetcher.addChattyMessageListener(m);
        }

        public void addEditionListener(NewEditionDelegate ne)
        {
            fetcher.addEditionListener(ne);
        }

        public void ChangeConfig(AppSettings conf)
        {
            this.conf = conf;
            log.WriteLine("new config");
        }

        public async Task waitUntil(DateTime t)
        {
            int d = (int)t.Subtract(DateTime.UtcNow).TotalMilliseconds;
            if (d > 0)
            {
                terseMessage("waiting until " + t.ToString("t"));
                await Task.Delay(d);
            }
        }

        public async Task republish()
        {
            await republish(schedule.current(DateTime.UtcNow));
        }

        public async Task republish(DateTime epoch)
        {
            await fetcher.reFetchIfNeeded(epoch);
            // (re-)publish it
            publisher.publish(fetcher.lastWeHave(), epoch);
        }
         
        public async Task<DateTime> fetchAndPublish(DateTime epoch)
        {
            DateTime? lmd = null;
            DateTime t = schedule.current(epoch);
            DateTime bc = t.AddMinutes(conf.BroadcastMinuteAfter);
            try
            {
                if (epoch < bc) // check if we have time to publish a late file
                {
                    await fetcher.reFetchIfNeeded(t);
                }
                else  // no we don't
                {
                    t = schedule.next(epoch);
                    // wait until a few minutes before publication
                    await waitUntil(t.AddMinutes(0 - conf.MinutesBefore));
                    await fetcher.reFetchIfNeeded(t);
                    bc = t.AddMinutes(conf.BroadcastMinuteAfter);
                }
                // publish most recent as the next edition in case we can't get the next one
                publisher.publish(fetcher.lastWeHave(), t);
                lmd = await fetcher.waitfor(t, bc);
                if (lmd == null)
                {
                    badMessage(t);
                }
                else
                {
                    await fetcher.save(t);
                    if(conf.UpdateAllEditions)
                    {
                        publishAll(fetcher.lastWeHave());
                    }
                    else
                    {
                        publisher.publish(fetcher.lastWeHave(), t);
                    }
                    terseMessage(t.ToString("HH:mm") + " edition published at " + lmd);
                }
            }
            catch (Exception ex)
            {
                terseMessage(ex.Message);
                log.WriteLine(ex.Message);
                // best delete the current file so we will fetch another
                FileInfo f = new FileInfo(fetcher.lastWeHave());
                f.Delete();
            }
            return bc;
        }

        public void publishAll(string path)
        {
            DateTime[] all = schedule.events(DateTime.UtcNow.Date);
            foreach(DateTime t in all)
            {
                publisher.publish(path, t);
            }
        }

        private void badMessage(DateTime t)
        {
            string message = "";
            FileInfo f = new FileInfo(fetcher.lastWeHave());
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
            terseMessage(message);
            log.WriteLine(message);
        }
    }
}
