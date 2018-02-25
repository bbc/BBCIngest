using System;
using System.Threading.Tasks;
using Ingest;
using System.IO;
using System.Net.Http;
using System.Net;

namespace BBCIngestOne
{
    class Program
    {
        static void Main(string[] args)
        {
            MainTask().Wait();
        }

        static async Task MainTask()
        {
            AppSettings conf = new AppSettings();
            conf.LoadAppSettings();
            conf.SaveAppSettings();
            Directory.CreateDirectory(conf.PublishFolder);
            Directory.CreateDirectory(conf.Archive);
            HttpClientHandler httpClientHandler = new HttpClientHandler()
            {
                Proxy = WebRequest.GetSystemWebProxy()
            };
            HttpClient hc = new HttpClient(httpClientHandler);
            FetchAndPublish fetcher = new FetchAndPublish(conf, hc);
            fetcher.listenForTerseMessages(new TerseMessageDelegate(Console.WriteLine));
            fetcher.listenForChattyMessages(new ChattyMessageDelegate(Console.WriteLine));
            fetcher.listenForEditionStatus(new ShowEditionStatusDelegate(Console.WriteLine));
            fetcher.addLogListener(new LogDelegate(Console.WriteLine));
            await fetcher.republish();
            try
            {
                DateTime bc = await fetcher.fetchAndPublish(DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
