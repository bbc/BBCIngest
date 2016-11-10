using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBCIngest
{
    class Logging
    {
        private StreamWriter log;

        public Logging(string path)
        {
            log = System.IO.File.AppendText(path);
        }

        internal void WriteLine(string logmessage)
        {
            log.WriteLine(DateTime.UtcNow.ToString() + " "+ logmessage);
            log.Flush();
        }
    }
}
