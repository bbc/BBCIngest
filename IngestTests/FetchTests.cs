using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ingest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace Ingest.Tests
{
    [TestClass()]
    public class FetchTests
    {
        [TestMethod()]
        public void FetchTest()
        {
        }

        [TestMethod()]
        public void addTerseMessageListenerTest()
        {
        }

        [TestMethod()]
        public void addChattyMessageListenerTest()
        {
        }

        [TestMethod()]
        public void addEditionListenerTest()
        {
        }

        [TestMethod()]
        public void addLogListenerTest()
        {
        }

        [TestMethod()]
        public void webnameTest()
        {
            AppSettings settings = new AppSettings();
            settings.Basename = "y";
            settings.Prefix = "http://x/";
            settings.Suffix = "wav";
            settings.Webdate = "";
            HttpClient hc = new HttpClient();
            Fetch uut = new Fetch(settings, hc);
            DateTime t = DateTime.Now;
            string s = uut.webname(t);
        }

        [TestMethod()]
        public void lastWeHaveTest()
        {
        }

        [TestMethod()]
        public void waitforTest()
        {
        }

        [TestMethod()]
        public void saveTest()
        {
        }

        [TestMethod()]
        public void latestPublishTimeTest()
        {
        }

        [TestMethod()]
        public void shouldRefetchTest()
        {
        }

        [TestMethod()]
        public void reFetchIfNeededTest()
        {
        }
    }
}