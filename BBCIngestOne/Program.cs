using System;
using System.Threading.Tasks;
using BBCIngest;
using System.IO;

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
            Directory.CreateDirectory(conf.Publish);
            Directory.CreateDirectory(conf.Archive);
            Directory.CreateDirectory(conf.Logfolder);
            FetchAndPublish fetcher = new FetchAndPublish(conf);
            fetcher.listenForTerseMessages(new TerseMessageDelegate(Console.WriteLine));
            fetcher.listenForChattyMessages(new ChattyMessageDelegate(Console.WriteLine));
            fetcher.listenForEditionStatus(new ShowEditionStatusDelegate(Console.WriteLine));
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
