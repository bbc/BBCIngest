using Microsoft.Win32.TaskScheduler;
using System;

namespace BBCIngest
{
    public class Schedule
    {
        private AppSettings conf;

        public Schedule(AppSettings conf)
        {
            this.conf = conf;
        }

        private int[] minutes()
        {
            string[] s = conf.Minutepattern.Split(',');
            int[] m = new int[s.Length];
            for(int i = 0; i<s.Length; i++)
            {
                int n;
                if (int.TryParse(s[i], out n))
                {
                    m[i] = n;
                }
            }
            return m;
        }

        private int[] hours()
        {
            int[] h;
            if (conf.Hourpattern == "*")
            {
                h = new int[24];
                for (int i=0; i<24; i++)
                {
                    h[i] = i;
                }
                return h;
            }
            string[] s = conf.Hourpattern.Split(',');
            h = new int[s.Length];
            for(int i = 0; i<s.Length; i++)
            {
                int n;
                if (int.TryParse(s[i], out n))
                {
                    h[i] = n;
                }
            }
            return h;
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
                else {
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
                if(t!= null)
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

        public DateTime[] events(DateTime start)
        {
            int[] hours = this.hours();
            int[] minutes = this.minutes();
            int n = minutes.Length * hours.Length;
            DateTime[] d = new DateTime[n];
            int i = 0;
            for(int h = 0; h<hours.Length; h++)
            {
                DateTime hour = start.AddHours(hours[h]);
                for(int m=0; m<minutes.Length; m++)
                {
                    d[i++] = hour.AddMinutes(minutes[m]);
                }
            }
            return d;
        }

        public DateTime next(DateTime t)
        {
            DateTime today = t.Date;
            DateTime[] all = events(today);
            for(int i=0; i<all.Length; i++)
            {
                DateTime ev = all[i];
                if (ev > t)
                {
                    return ev;
                }
            }
            // it might be tomorrow
            all = events(today.AddDays(1));
            if(all.Length>0)
            {
                return all[0];
            }
            throw new Exception("no events");
        }

        public DateTime current(DateTime t)
        {
            DateTime today = t.Date;
            DateTime[] all = events(today);
            for (int i=all.Length-1; i>=0; i--)
            {
                DateTime ev = all[i];
                if (ev < t)
                {
                    return ev;
                }
            }
            // it might be yesterday
            all = events(today.AddDays(-1));
            if (all.Length > 0)
            {
                return all[all.Length-1];
            }
            throw new Exception("no events");
        }
    }
}
