using System;
using System.IO;

namespace Ingest
{
    public interface IPublishSettings
    {
        bool SafePublishing { get; set; }
        string Publish { get; set; }
        string Basename { get; set; }
        string Discdate { get; set; }
        string Suffix { get; set; }
        bool UseLocaltime { get; set; }
        bool UpdateAllEditions { get; set; }
    }

    class Publish
    {
        private event TerseMessageDelegate terseMessage;
        private IPublishSettings conf;

        public Publish(IPublishSettings conf)
        {
            this.conf = conf;
        }

        public void addTerseMessageListener(TerseMessageDelegate m)
        {
            this.terseMessage += m;
        }

        public string discname(DateTime t)
        {
            string s = "";
            if (conf.Discdate != "") // allow empty Discdate to force fixed discname
            {
                if (conf.UseLocaltime)
                {
                    s = t.ToLocalTime().ToString(conf.Discdate);
                }
                else
                {
                    s = t.ToString(conf.Discdate);
                }
            }
            return conf.Basename + s + "." + conf.Suffix;
        }

        public void publish(string path, DateTime epoch, DateTime[] all)
        {
            publishOne(path, epoch);
            if(conf.UpdateAllEditions)
            {
                publishAllButOne(path, epoch, all);
            }
        }

        public void publishOne(string path, DateTime t)
        {
            FileInfo f = new FileInfo(path);
            if (f.Exists == false)
            {
                return;
            }
            string savename = conf.Publish + discname(t);
            if (conf.SafePublishing)
            {
                string tempname = conf.Publish + conf.Basename + ".tmp";
                try
                {
                    FileInfo pf = f.CopyTo(tempname, true);
                    // if no exception we are OK to overwrite
                    System.IO.File.Delete(savename);
                    pf.MoveTo(savename);
                }
                catch
                {
                    terseMessage("error writing to " + conf.Publish + " folder");
                }
            }
            else
            {
                f.CopyTo(savename, true);
            }
        }

        public void publishAll(string path, DateTime[] times)
        {
            foreach (DateTime t in times)
            {
                publishOne(path, t);
            }
        }

        public void publishAllButOne(string path, DateTime theOne, DateTime[] times)
        {
            foreach (DateTime t in times)
            {
                if (!t.Equals(theOne))
                {
                    publishOne(path, t);
                }
            }
        }

    }
}
