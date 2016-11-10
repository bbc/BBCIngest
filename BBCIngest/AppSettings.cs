using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace BBCIngest
{
    public class AppSettings
    {
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
                return addPathSeparatorIfNeeded(publish);
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
                return addPathSeparatorIfNeeded(archive);
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
                return addPathSeparatorIfNeeded(logfolder);
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

        public bool appSettingsChanged { get; set; }

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

        private string addPathSeparatorIfNeeded(string p)
        {
            if (p.EndsWith("" + Path.PathSeparator))
            {
                return p;
            }
            return p + Path.PathSeparator;
        }
    }
}
