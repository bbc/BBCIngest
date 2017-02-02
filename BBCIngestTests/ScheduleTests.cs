using Microsoft.VisualStudio.TestTools.UnitTesting;
using BBCIngest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.TaskScheduler;

namespace BBCIngest.Tests
{
    [TestClass()]
    public class ScheduleTests
    {
        [TestMethod()]
        public void ScheduleTest()
        {
            AppSettings conf = new AppSettings();
            conf.Hourpattern = "*";
            conf.Minutepattern = "00,30";
            Schedule uut = new Schedule(conf);
            Assert.IsNotNull(uut);
        }

        [TestMethod()]
        public void TaskSchedulerTest()
        {
            AppSettings conf = new AppSettings();
            conf.Hourpattern = "*";
            conf.Minutepattern = "00,30";
            Schedule uut = new Schedule(conf);
            uut.deleteTaskAndTriggers();
            using (TaskService ts = new TaskService())
            {
                Assert.IsNull(ts.GetTask("BBCIngest"));
            }
            uut.createTaskAndTriggers(@"C:\WINDOWS\system32\cmd.exe");
            using (TaskService ts = new TaskService())
            {
                Microsoft.Win32.TaskScheduler.Task t = ts.GetTask("BBCIngest");
                Assert.IsNotNull(t);
                DateTime sod = DateTime.UtcNow.Date;
                // getruntimes is exclusive of the start time
                DateTime[] runtimes = t.GetRunTimes(sod.AddMilliseconds(-1), sod.AddMilliseconds(-1).AddDays(1));
                Assert.AreEqual(48, runtimes.Length);
                DateTime[] events = uut.events(sod);
                Assert.AreEqual(48, events.Length);
                for(int i=0; i<48; i++)
                {
                    Assert.AreEqual(events[i], runtimes[i]);
                }
            }
            uut.deleteTaskAndTriggers();
            using (TaskService ts = new TaskService())
            {
                Assert.IsNull(ts.GetTask("BBCIngest"));
            }
        }

        [TestMethod()]
        public void nextTest()
        {
            AppSettings conf = new AppSettings();
            conf.Hourpattern = "*";
            conf.Minutepattern = "00,30";
            Schedule uut = new Schedule(conf);
            DateTime t = new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            for (int i = 0; i < 24; i++)
            {
                for (int m = 0; m <= 30; m += 30)
                {
                    DateTime x = t.AddHours(i).AddMinutes(m);
                    Assert.AreEqual(x.AddMinutes(30), uut.next(x));
                }
            }
        }

        [TestMethod()]
        public void currentTest()
        {
            AppSettings conf = new AppSettings();
            conf.Hourpattern = "*";
            conf.Minutepattern = "00,30";
            Schedule uut = new Schedule(conf);
            DateTime t = DateTime.UtcNow.Date;
            for (int i = 0; i < 24; i++)
            {
                for (int m = 0; m <= 30; m += 30)
                {
                    DateTime x = t.AddHours(i).AddMinutes(m);
                    Assert.AreEqual(x, uut.current(x.AddMinutes(30)));
                }
            }
        }
    }
}