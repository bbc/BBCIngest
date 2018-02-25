using Microsoft.Win32.TaskScheduler;
using System;

namespace Ingest
{
    public class Win32ScheduleInstaller : IScheduleInstaller
    {
        private event TerseMessageDelegate terseMessage;
        private event LogDelegate logger;
        private Schedule schedule;

        public Win32ScheduleInstaller(Schedule schedule)
        {
            this.schedule = schedule;
        }

        public void addTerseMessageListener(TerseMessageDelegate fm)
        {
            this.terseMessage += fm;
        }

        public void addLogListener(LogDelegate logDelegate)
        {
            this.logger += logDelegate;
        }

        public bool installTask(string execPath, string arguments)
        {
            if (schedule.conf.RunAsService)
                return installTaskAsService(execPath, arguments);
            else
            {
                installUserTask(execPath, arguments);
                return true;
            }
                
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

        private bool installTaskAsService(string execPath, string arguments)
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
                    terseMessage("Either set RunAsService false in settings or run this program with Admin privileges");
                    logger("failed to install service");
                    return false;
                }
                logger($"installed task {schedule.conf.TaskName} as service");
                return true;
            }
        }

        private void installUserTask(string execPath, string arguments)
        {
            TaskDefinition td = createTaskDefinition(execPath, arguments);
            using (TaskService ts = new TaskService())
            {
                // Register the task in the root folder
                ts.RootFolder.RegisterTaskDefinition(schedule.conf.TaskName, td);
            }
            logger($"installed user task {schedule.conf.TaskName}");
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
                return (new TaskService()).GetTask(schedule.conf.TaskName) != null;
            }           
        }
    }
}
