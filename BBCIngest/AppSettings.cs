using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace BBCIngest
{
    public class AppSettings
    {
        public bool appSettingsChanged;
        private string defaultDir;

        private string archive;
        public string Archive
        {
            get
            {
                return addDirectorSeparatorIfNeeded(archive);
            }

            set
            {
                archive = value;
            }
        }

        [CategoryAttribute("Logging")]
        public string City { get; set; }

        [CategoryAttribute("Logging")]
        public string Station { get; set; }

        [CategoryAttribute("Logging")]
        private string logfolder;
        public string Logfolder
        {
            get
            {
                return addDirectorSeparatorIfNeeded(logfolder);
            }

            set
            {
                logfolder = value;
            }
        }

        [CategoryAttribute("Logging")]
        public string LogUrl { get; set; }

        [CategoryAttribute("Logging")]
        public bool PostLogs { get; set; }

        [CategoryAttribute("Source")]
        public int MinutesBefore { get; set; }

        [CategoryAttribute("Source")]
        public string Basename { get; set; }

        [CategoryAttribute("Source")]
        public string Prefix { get; set; }

        [CategoryAttribute("Source")]
        public string Webdate { get; set; }
        [CategoryAttribute("Source")]
        public string Minutepattern { get; set; }

        [CategoryAttribute("Source")]
        public string Hourpattern { get; set; }

        [CategoryAttribute("Source")]
        public string Suffix { get; set; }
        [CategoryAttribute("Target")]
        public string Discdate { get; set; }

        [CategoryAttribute("Target")]
        public bool UseLocaltime { get; set; }

        [CategoryAttribute("Target")]
        public int BroadcastMinuteAfter { get; set; }

        [CategoryAttribute("Target")]
        public bool SafePublishing { get; set; }

        private string publish;
        [CategoryAttribute("Target")]
        public string Publish
        {
            get
            {
                return addDirectorSeparatorIfNeeded(publish);
            }

            set
            {
                publish = value;
            }
        }

        public int ValueWidth()
        {
            int w = 0;
            PropertyInfo[] p = this.GetType().GetProperties();
            for (int i = 0; i < p.Length; i++)
            {
                Object o = p[i].GetValue(this);
                if(o != null)
                {
                    int l = o.ToString().Length;
                    if (l > w)
                        w = l;
                }
            }
            return w;
        }

        public int LabelWidth()
        {
            int w = 0;
            PropertyInfo[] p = this.GetType().GetProperties();
            for (int i = 0; i < p.Length; i++)
            {
                int l = p[i].Name.Length;
                if (l > w)
                    w = l;
            }
            return w;
        }

        public string webname(DateTime t)
        {
            return Basename + t.ToString(Webdate) + "." + Suffix;
        }

        public string discname(DateTime t)
        {
            string s = "";
            if (Discdate != "")
            {
                if (UseLocaltime)
                {
                    s = t.ToLocalTime().ToString(Discdate);
                }
                else
                {
                    s = t.ToString(Discdate);
                }
            }
            return Basename + s + "." + Suffix;
        }

        public string latest()
        {
            return Archive + Basename + "." + Suffix;
        }

        public AppSettings ShallowCopy()
        {
            return (AppSettings)this.MemberwiseClone();
        }

        public bool LoadAppSettings()
        {
            string settingsPath = System.Windows.Forms.Application.LocalUserAppDataPath;
            XmlSerializer sz = null;
            FileStream fs = null;
            bool fileExists = false;
            try
            {
                sz = new XmlSerializer(typeof(AppSettings));
                FileInfo fi = new FileInfo(settingsPath+@"\BBCIngest.config");
                if (fi.Exists)
                {
                    fs = fi.OpenRead();
                    AppSettings mas = (AppSettings)sz.Deserialize(fs);
                    fileExists = true;
                    PropertyInfo[] p = this.GetType().GetProperties();
                    for (int i=0; i<p.Length; i++)
                    {
                        p[i].SetValue(this, p[i].GetValue(mas));
                    }
                    // or use ShallowCopy?
                }
                else
                {
                    Logfolder = settingsPath; // @"C:\log\";
                    Archive = settingsPath; // @"C:\archive\";

                    MinutesBefore = 4;
                    Prefix = "http://wsodprogrf.bbc.co.uk/bd/tx/bbcminute/mp3/";
                    Basename = "bbcminute";
                    Webdate = "yyMMddHHmm";
                    Suffix = "mp3";

                    Hourpattern = "*";
                    Minutepattern = "00,30";

                    Publish = @"C:\source\";
                    Discdate = "HHmm";
                    BroadcastMinuteAfter = 0;

                    PostLogs = true;
                    LogUrl = "";
                    City = "London";
                    Station = "BBC World Service";
                }
            }
            catch(Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }
            if (defaultDir == null)
            {
                defaultDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                this.appSettingsChanged = true;
            }
            return fileExists;
        }

        public bool SaveAppSettings()
        {
            if (this.appSettingsChanged)
            {
                XmlSerializer sz = new XmlSerializer(typeof(AppSettings));
                StreamWriter sw = new StreamWriter(System.Windows.Forms.Application.LocalUserAppDataPath + @"\BBCIngest.config", false);
                if (sw != null)
                {
                    sz.Serialize(sw, this);
                    sw.Close();
                }
                sw.Dispose();
            }
            return appSettingsChanged;
        }

        private string addDirectorSeparatorIfNeeded(string p)
        {
            char e = p[p.Length - 1];
            if (e == Path.DirectorySeparatorChar)
            {
                return p;
            }
            return p + Path.DirectorySeparatorChar;
        }
    }
}
