using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;

namespace Ingest.Tests
{
    [TestClass()]
    public class PublishTests
    {

        [TestMethod()]
        public void discnameTest()
        {
            DateTime t = new DateTime(2017, 1, 1, 0, 0, 0);
            IPublishSettings conf = new AppSettings();
            conf.Basename = "test";
            conf.Discdate = "HHmm";
            Publish uut = new Publish(conf);
            Assert.AreEqual(conf.Basename+"0000."+conf.Extension, uut.discname(t));
        }

        [TestMethod()]
        public void transCodeToTest()
        {
            IPublishSettings conf = new AppSettings();
            conf.Basename = "test";
            conf.Discdate = "HHmm";
            Publish uut = new Publish(conf);
            ProcessStartInfo startInfo = uut.getPSI("1", "2");
            Assert.AreEqual("ffmpeg.exe", startInfo.FileName);
            Assert.AreEqual("-i 1 -ar 44100 -b 256k -acodec libtwolame -f mp2 2", startInfo.Arguments);
            startInfo.FileName = Directory.GetCurrentDirectory()+@"\ffmpeg.exe";
            uut.encodeMP2(startInfo);
        }
    }
}