using Microsoft.Win32.TaskScheduler;
using System;

namespace Ingest
{
    public class Win32ScheduleInstaller : IScheduleInstaller
    {
        private Schedule schedule;

        public Win32ScheduleInstaller(Schedule schedule)
        {
            this.schedule = schedule;
        }

        private TaskDefinition createTaskDefinition(string execPath, string arguments)
        {
            using (TaskService ts = new TaskService())
            {
                // Create a new task definition and assign properties
                TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Description = "Run BBCIngest";

                td.Principal.LogonType = TaskLogonType.InteractiveToken;
                td.Settings.DisallowStartIfOnBatteries = false;
                td.Settings.RunOnlyIfNetworkAvailable = true;
                td.Settings.RunOnlyIfIdle = false;
                td.Settings.DisallowStartIfOnBatteries = false;
                td.Settings.WakeToRun = true;

                int[] minutes = schedule.minutes();
                if (schedule.conf.Hourpattern == "*")
                {
                    // one trigger for each specified minute repeating each hour
                    for (int m = 0; m < minutes.Length; m++)
                    {
                        TimeTrigger dt = new TimeTrigger();
                        dt.StartBoundary = DateTime.UtcNow.Date
                            .AddMinutes(minutes[m])
                            .AddMinutes(-schedule.conf.MinutesBefore);
                        dt.Repetition.Interval = TimeSpan.FromHours(1);
                        td.Triggers.Add(dt);
                    }
                }
                else
                {
                    // one trigger for each specified minute/hour combination, repeating daily
                    string[] s = schedule.conf.Hourpattern.Split(',');
                    for (int i = 0; i < s.Length; i++)
                    {
                        int h;
                        if (int.TryParse(s[i], out h))
                        {
                            for (int m = 0; m < schedule.minutes().Length; m++)
                            {
                                DailyTrigger dt = new DailyTrigger();
                                dt.StartBoundary = DateTime.UtcNow.Date
                                    .AddHours(h)
                                    .AddMinutes(minutes[m])
                                    .AddMinutes(-schedule.conf.MinutesBefore);
                                td.Triggers.Add(dt);
                            }
                        }
                    }
                }

                // Add an action that will launch BBCIngest whenever the trigger fires
                td.Actions.Add(new ExecAction(execPath, arguments, null));
                return td;
            }
        }

        public bool installTaskAsService(string execPath, string arguments)
        {
            TaskDefinition td = createTaskDefinition(execPath, arguments);
            using (TaskService ts = new TaskService())
            {
                // Register the task in the root folder
                try
                {
                    ts.RootFolder.RegisterTaskDefinition(schedule.conf.TaskName, td,
                       TaskCreation.CreateOrUpdate, "SYSTEM", null,
                        TaskLogonType.ServiceAccount);
                }
                catch (System.UnauthorizedAccessException e)
                {
                    Console.WriteLine(e);
                    return false;
                }
                return true;
            }
        }

        public void installUserTask(string execPath, string arguments)
        {
            TaskDefinition td = createTaskDefinition(execPath, arguments);
            using (TaskService ts = new TaskService())
            {
                // Register the task in the root folder
                ts.RootFolder.RegisterTaskDefinition(schedule.conf.TaskName, td);
            }
        }

        public void runTask()
        {
            using (TaskService ts = new TaskService())
            {
                Task t = ts.GetTask(schedule.conf.TaskName);
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
                if (ts.GetTask(schedule.conf.TaskName) != null)
                {
                    ts.RootFolder.DeleteTask(schedule.conf.TaskName);
                }
            }
        }

        public bool IsInstalled {
            get
            {
                DateTime? next = nextRun();            
                return next !=null;
            }            
        }

        public DateTime? nextRun()
        {
            using (TaskService ts = new TaskService())
            {
                Task t = ts.GetTask(schedule.conf.TaskName);
                if (t != null)
                {
                    return t.NextRunTime;
                }
            }
            return null;
        }
    }
}
