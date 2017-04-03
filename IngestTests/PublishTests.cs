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
            Assert.AreEqual(conf.Basename+"0000."+conf.Suffix, uut.discname(t));
        }

        [TestMethod()]
        public void transCodeToTest()
        {
            IPublishSettings conf = new AppSettings();
            conf.Basename = "test";
            conf.Discdate = "HHmm";
            Publish uut = new Publish(conf);
            ProcessStartInfo startInfo = uut.getPSI("1", "2", Codec.mp2);
            Assert.AreEqual("ffmpeg.exe", startInfo.FileName);
            Assert.AreEqual("-i 1 -acodec mp2 2", startInfo.Arguments);
            startInfo.FileName = Directory.GetCurrentDirectory()+@"\ffmpeg.exe";
            uut.transCodeTo(startInfo);
        }
    }
}