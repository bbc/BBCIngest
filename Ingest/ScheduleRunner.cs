using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ingest
{
    public class ScheduleRunner : Schedule
    {
        public ScheduleRunner(IScheduleSettings conf) : base(conf)
        {
        }

        public DateTime current(DateTime t)
        {
            DateTime today = t.Date;
            DateTime[] all = events(today);
            for (int i = all.Length - 1; i >= 0; i--)
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
                return all[all.Length - 1];
            }
            throw new Exception("no events");
        }
    }
}
