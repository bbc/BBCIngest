using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;

namespace Ingest
{
    public interface IPublishSettings
    {
        bool SafePublishing { get; set; }
        string Publish { get; set; }
        string Basename { get; set; }
        string Extension { get; set; }
        string Discdate { get; set; }
        bool UseLocaltime { get; set; }
        bool UpdateAllEditions { get; set; }
    }

    public class Publish
    {
        private event TerseMessageDelegate terseMessage;
        private event ChattyMessageDelegate chattyMessage;
        private IPublishSettings conf;

        public Publish(IPublishSettings conf)
        {
            this.conf = conf;
        }

        public void addTerseMessageListener(TerseMessageDelegate m)
        {
            this.terseMessage += m;
        }

        public void addChattyMessageListener(ChattyMessageDelegate fm)
        {
            this.chattyMessage += fm;
        }

        public string discname(DateTime t)
        {
            return discbasename(t) + "." + conf.Extension;
        }

        public string discbasename(DateTime t)
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
            return conf.Basename + s;
        }

        public void publish(string path, DateTime epoch, DateTime[] all)
        {
            chattyMessage("Publishing ...");
            publishOne(path, epoch);
            if (conf.UpdateAllEditions && conf.Discdate.Length > 0)
            {
                publishAllButOne(path, epoch, all);
            }
        }

        public void publishOne(string source, DateTime t)
        {
            FileInfo f = new FileInfo(source);
            if (f.Exists == false)
            {
                return;
            }
            string savename = conf.Publish + discname(t);
            string tempname = savename;
            if (conf.SafePublishing)
            {
                tempname = conf.Publish + conf.Basename + ".tmp";
            }
            FileInfo pf = new FileInfo(tempname);
            if (conf.Extension == "mp2")
            {
                encodeMP2(source, tempname);
            }
            else
            {
                pf = f.CopyTo(tempname, true);
            }
            if (conf.SafePublishing)
            {
                System.IO.File.Delete(savename);
                pf.MoveTo(savename);
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

        public void encodeMP2(string source, string dest)
        {
            encodeMP2(getPSI(source, dest));
        }

        public ProcessStartInfo getPSI(string source, string dest)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            Assembly a = Assembly.GetEntryAssembly();
            if (a == null)
            {
                startInfo.FileName = "ffmpeg.exe";
            }
            else
            {
                FileInfo fi = new FileInfo(a.Location);
                startInfo.FileName = fi.DirectoryName + @"\ffmpeg.exe";
            }
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "-i " + source + " -ar 44100 -b 256k -acodec libtwolame -f mp2 " + dest;
            return startInfo;
        }

        public void encodeMP2(ProcessStartInfo startInfo)
        {
            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                // Log error.
                terseMessage(ex.ToString());
            }
        }
    }
}