using System;
using System.Collections.Generic;

namespace Ingest
{
    public class ScheduleInstaller : IScheduleInstaller
    {
        private class Repetition {
            public TimeSpan Interval;
        }

        private class Trigger {
            public DateTime StartBoundary;
            public Repetition Repetition = new Repetition();
            public override string ToString(){
                return $"{StartBoundary} {Repetition.Interval}";
            }
        }

        private class DailyTrigger : Trigger {
        }

        private class TimeTrigger : Trigger {
        }

        private class Action {
        }

        private class ExecAction: Action {
            String a,b,c;

            public ExecAction(String a, String b, String c) {
                this.a=a;
                this.b=b;
                this.c=c;
            }

            public override string ToString(){
                return $"{a} {b} {c}";
            }
        }
        
        private class TaskDefinition {
            public List<Trigger> Triggers = new List<Trigger>();
            public List<Action> Actions = new List<Action>();
            public override string ToString(){
                string triggers = "";
                foreach (Trigger item in Triggers){
                    triggers += item.ToString() + ", ";
                }
                string actions = "";
                foreach (Action item in Actions){
                    actions += item.ToString() + ", ";
                }
                return $"Triggers:{triggers}Actions:{actions}";
            }

        }

        private Schedule schedule;
        private bool installed = false;

        public bool IsInstalled {
            get
            {
                return installed;
            }            
        }

        public ScheduleInstaller(Schedule schedule)
        {
            this.schedule = schedule;
        }

        private TaskDefinition createTaskDefinition(string execPath, string arguments)
        {
            // Create a new task definition and assign properties
            TaskDefinition td = new TaskDefinition();

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
                        for (int m = 0; m < minutes.Length; m++)
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

        public bool installTaskAsService(string execPath, string arguments)
        {
            TaskDefinition td = createTaskDefinition(execPath, arguments);
            Console.WriteLine($"installTaskAsService {schedule.conf.TaskName}, {td}");
            installed = true;
            return true;
        }

        public void installUserTask(string execPath, string arguments)
        {
            TaskDefinition td = createTaskDefinition(execPath, arguments);
            Console.WriteLine($"installUserTask {schedule.conf.TaskName}, {td}");
            installed = true;
        }

        public void runTask()
        {
            Console.WriteLine($"runTask {schedule.conf.TaskName}");
        }

        public void deleteTaskAndTriggers()
        {
            Console.WriteLine($"deleteTaskAndTriggers {schedule.conf.TaskName}");
            installed = false;
        }

        public DateTime? nextRun()
        {
            return schedule.next();
        }
    }
}
