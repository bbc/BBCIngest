using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;

namespace Ingest
{
    public enum Codec { None, mp2, mp3 };

    public interface IPublishSettings
    {
        bool SafePublishing { get; set; }
        string Publish { get; set; }
        string Basename { get; set; }
        string Discdate { get; set; }
        string Suffix { get; set; }
        bool UseLocaltime { get; set; }
        bool UpdateAllEditions { get; set; }
        Codec Transcode { get; set; }
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
            chattyMessage("Publishing ...");
            publishOne(path, epoch);
            if (conf.UpdateAllEditions && conf.Discdate.Length > 0)
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
            string tempname = savename;
            if (conf.SafePublishing)
            {
                tempname = conf.Publish + conf.Basename + ".tmp";
            }
            FileInfo pf = new FileInfo(tempname);
            if (conf.Transcode == Codec.None)
            {
                pf = f.CopyTo(tempname, true);
            }
            else
            {
                transCodeTo(path, tempname, conf.Transcode);

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

        public ProcessStartInfo getPSI(string source, string dest, Codec codec)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            Assembly a = Assembly.GetEntryAssembly();
            if(a == null)
            {
                startInfo.FileName = "ffmpeg.exe";
            }
            else
            {
                startInfo.FileName = a.Location + "\\ffmpeg.exe";
            }
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "-i " + source + " -acodec " + codec + " " + dest;
            return startInfo;
        }

        public void transCodeTo(string source, string dest, Codec codec)
        {
            ProcessStartInfo startInfo = getPSI(source, dest, codec);
            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                }
            }
            catch
            {
                // Log error.
            }
        }
    }
}
