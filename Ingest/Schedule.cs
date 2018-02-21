using Microsoft.Win32.TaskScheduler;
using System;

namespace Ingest
{
    public interface IScheduleSettings
    {
        string TaskName { get; set; }
        string Minutepattern { get; set; }
        string Hourpattern { get; set; }
        int MinutesBefore { get; set; }
    }

    public class Schedule
    {
        protected IScheduleSettings conf;

        public Schedule(IScheduleSettings conf)
        {
            this.conf = conf;
        }

        protected int[] minutes()
        {
            string[] s = conf.Minutepattern.Split(',');
            int[] m = new int[s.Length];
            for (int i = 0; i < s.Length; i++)
            {
                int n;
                if (int.TryParse(s[i], out n))
                {
                    m[i] = n;
                }
            }
            return m;
        }

        protected int[] hours()
        {
            int[] h;
            if (conf.Hourpattern == "*")
            {
                h = new int[24];
                for (int i = 0; i < 24; i++)
                {
                    h[i] = i;
                }
                return h;
            }
            string[] s = conf.Hourpattern.Split(',');
            h = new int[s.Length];
            for (int i = 0; i < s.Length; i++)
            {
                int n;
                if (int.TryParse(s[i], out n))
                {
                    h[i] = n;
                }
            }
            return h;
        }

        public DateTime[] events(DateTime start)
        {
            int[] hours = this.hours();
            int[] minutes = this.minutes();
            int n = minutes.Length * hours.Length;
            DateTime[] d = new DateTime[n];
            int i = 0;
            for (int h = 0; h < hours.Length; h++)
            {
                DateTime hour = start.AddHours(hours[h]);
                for (int m = 0; m < minutes.Length; m++)
                {
                    d[i++] = hour.AddMinutes(minutes[m]);
                }
            }
            return d;
        }
    }
}
