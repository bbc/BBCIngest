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
            Main().Wait();
        }

        static async Task Main()
        {
            AppSettings conf = new AppSettings();
            conf.LoadAppSettings();
            conf.SaveAppSettings();
            Directory.CreateDirectory(conf.Publish);
            Directory.CreateDirectory(conf.Archive);
            Directory.CreateDirectory(conf.Logfolder);
            FetchAndPublish fetcher = new FetchAndPublish(conf);
            fetcher.addMessageListener(new FetchMessageDelegate(Console.WriteLine));
            fetcher.addEditionListener(new NewEditionDelegate(Console.WriteLine));
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
