using Microsoft.Win32.TaskScheduler;
using System;

namespace Ingest
{
    public class ScheduleInstaller : Schedule
    {
        public ScheduleInstaller(IScheduleSettings conf) : base(conf)
        {
        }

        public void createTaskAndTriggers(String execPath)
        {
            using (TaskService ts = new TaskService())
            {
                // Create a new task definition and assign properties
                TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Description = "Run BBCIngest";

                td.Principal.LogonType = TaskLogonType.InteractiveToken;
                int[] minutes = this.minutes();
                if (conf.Hourpattern == "*")
                {
                    // one trigger for each specified minute repeating each hour
                    for (int m = 0; m < minutes.Length; m++)
                    {
                        TimeTrigger dt = new TimeTrigger();
                        dt.StartBoundary = DateTime.UtcNow.Date
                            .AddMinutes(minutes[m])
                            .AddMinutes(-conf.MinutesBefore);
                        dt.Repetition.Interval = TimeSpan.FromHours(1);
                        td.Triggers.Add(dt);
                    }
                }
                else
                {
                    // one trigger for each specified minute/hour combination, repeating daily
                    string[] s = conf.Hourpattern.Split(',');
                    for (int i = 0; i < s.Length; i++)
                    {
                        int h;
                        if (int.TryParse(s[i], out h))
                        {
                            for (int m = 0; m < minutes.Length; m++)
                            {
                                DailyTrigger dt = new DailyTrigger();
                                dt.StartBoundary = DateTime.UtcNow.Date
                                    .AddHours(h)
                                    .AddMinutes(minutes[m])
                                    .AddMinutes(-conf.MinutesBefore);
                                td.Triggers.Add(dt);
                            }
                        }
                    }
                }

                // Add an action that will launch BBCIngest whenever the trigger fires
                td.Actions.Add(new ExecAction(execPath, "once", null));

                // Register the task in the root folder
                const string taskName = "BBCIngest";
                ts.RootFolder.RegisterTaskDefinition(taskName, td);
            }
        }

        public void runTask()
        {
            using (TaskService ts = new TaskService())
            {
                Task t = ts.GetTask("BBCIngest");
                if (t != null)
                {
                    t.Run();
                }
            }
        }

        public void deleteTaskAndTriggers()
        {
            using (TaskService ts = new TaskService())
            {
                if (ts.GetTask("BBCIngest") != null)
                {
                    ts.RootFolder.DeleteTask("BBCIngest");
                }
            }
        }

        public DateTime? nextRun()
        {
            using (TaskService ts = new TaskService())
            {
                Task t = ts.GetTask("BBCIngest");
                if (t != null)
                {
                    return t.NextRunTime;
                }
            }
            return null;
        }
    }
}
