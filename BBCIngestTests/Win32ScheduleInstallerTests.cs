using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ingest;
using System;
using Microsoft.Win32.TaskScheduler;

namespace BBCIngestTests
{
    [TestClass()]
    class Win32ScheduleInstallerTests
    {
        [TestMethod()]
        public void TaskSchedulerTest()
        {
            AppSettings conf = new AppSettings();
            conf.Hourpattern = "*";
            conf.Minutepattern = "00,30";
            Schedule schedule = new Schedule(conf);
            Win32ScheduleInstaller uut = new Win32ScheduleInstaller(schedule);
            uut.deleteTaskAndTriggers();
            using (TaskService ts = new TaskService())
            {
                Assert.IsNull(ts.GetTask("BBCIngest"));
            }
            uut.installUserTask(@"C:\WINDOWS\system32\cmd.exe", "");
            using (TaskService ts = new TaskService())
            {
                Microsoft.Win32.TaskScheduler.Task t = ts.GetTask("BBCIngest");
                Assert.IsNotNull(t);
                DateTime sod = DateTime.UtcNow.Date;
                // getruntimes is exclusive of the start time
                DateTime[] runtimes = t.GetRunTimes(sod.AddMilliseconds(-1), sod.AddMilliseconds(-1).AddDays(1));
                Assert.AreEqual(48, runtimes.Length);
                DateTime[] events = schedule.events(sod);
                Assert.AreEqual(48, events.Length);
                for (int i = 0; i < 48; i++)
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
    }
}
