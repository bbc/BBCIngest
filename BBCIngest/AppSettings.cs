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
        private int minutesBefore = 4;
        private string basename = "bbcminute";
        private string prefix = "http://wsodprogrf.bbc.co.uk/bd/tx/bbcminute/mp3/";
        private string webdate = "yyMMddHHmm";
        private string discdate = "HHmm";
        private string minutepattern = "00,30";
        private string hourpattern = "*";
        private string city = "London";
        private string station = "BBC World Service";
        private string publish = @"C:\source\";
        private string archive = @"C:\archive\";
        private string logfolder = @"C:\log\";
        private string defaultDir;
        private bool useLocaltime = false;
        private string suffix = "mp3";
        private int broadcastMinuteAfter = 0;

        [CategoryAttribute("Source")]
        public int MinutesBefore
        {
            get
            {
                return minutesBefore;
            }

            set
            {
                minutesBefore = value;
            }
        }

        [CategoryAttribute("Source")]
        public string Basename
        {
            get
            {
                return basename;
            }

            set
            {
                basename = value;
            }
        }

        [CategoryAttribute("Source")]
        public string Prefix
        {
            get
            {
                return prefix;
            }

            set
            {
                prefix = value;
            }
        }

        [CategoryAttribute("Source")]
        public string Webdate
        {
            get
            {
                return webdate;
            }

            set
            {
                webdate = value;
            }
        }

        [CategoryAttribute("Target")]
        public string Discdate
        {
            get
            {
                return discdate;
            }

            set
            {
                discdate = value;
            }
        }

        [CategoryAttribute("Station")]
        public string City
        {
            get
            {
                return city;
            }

            set
            {
                city = value;
            }
        }

        [CategoryAttribute("Station")]
        public string Station
        {
            get
            {
                return station;
            }

            set
            {
                station = value;
            }
        }

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

        [CategoryAttribute("Source")]
        public string Minutepattern
        {
            get
            {
                return minutepattern;
            }

            set
            {
                minutepattern = value;
            }
        }

        [CategoryAttribute("Source")]
        public string Hourpattern
        {
            get
            {
                return hourpattern;
            }

            set
            {
                hourpattern = value;
            }
        }


        [CategoryAttribute("Target")]
        public bool UseLocaltime
        {
            get
            {
                return useLocaltime;
            }

            set
            {
                useLocaltime = value;
            }
        }

        [CategoryAttribute("Source")]
        public string Suffix
        {
            get
            {
                return suffix;
            }

            set
            {
                suffix = value;
            }
        }

        [CategoryAttribute("Target")]
        public int BroadcastMinuteAfter
        {
            get
            {
                return broadcastMinuteAfter;
            }

            set
            {
                broadcastMinuteAfter = value;
            }
        }

        public int ValueWidth()
        {
            int w = 0;
            PropertyInfo[] p = this.GetType().GetProperties();
            for (int i = 0; i < p.Length; i++)
            {
                int l = p[i].GetValue(this).ToString().Length;
                if (l > w)
                    w = l;
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

        public bool LoadAppSettings()
        {
            XmlSerializer sz = null;
            FileStream fs = null;
            bool fileExists = false;
            try
            {
                sz = new XmlSerializer(typeof(AppSettings));
                FileInfo fi = new FileInfo(System.Windows.Forms.Application.LocalUserAppDataPath+@"\BBCIngest.config");
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
