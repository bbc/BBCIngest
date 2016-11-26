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
/*
        private DateTime today(DateTime t)
        {
            return t.Date;
        }

        private DateTime yesterday(DateTime t)
        {
            return t.Date.AddDays(-1);
        }

        private DateTime tomorrow(DateTime t)
        {
            return t.Date.AddDays(1);
        }
        */
        private DateTime[] events(DateTime start)
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
