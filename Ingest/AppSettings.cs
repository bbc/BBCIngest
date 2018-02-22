using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using static System.Environment;


namespace Ingest
{
    public class AppSettings : IPublishSettings, IFetchSettings, IScheduleSettings
    {
        private string appName = "BBCIngest";
        public bool appSettingsChanged;
        private string defaultDir;
        private static string defaultSettingsPath = GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public string DefaultSettingsPath
        {
            get
            {
                return defaultSettingsPath;
            }
        }
        
        private string settingsPath = defaultSettingsPath;
        public string SettingsPath
        {
            get
            {
                return settingsPath;
            }
            set
            {
                settingsPath = value;
            }
        }


        private event TerseMessageDelegate terseMessage;

        public void addTerseMessageListener(TerseMessageDelegate m)
        {
            this.terseMessage += m;
        }

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

        [CategoryAttribute("Run")]
        public bool RunInForeground { get; set; }
        [CategoryAttribute("Run")]
        public bool RunAsService { get; set; }
        [CategoryAttribute("Run")]
        public string TaskName
        {
            get
            {
                if(settingsPath == defaultSettingsPath) {
                    return appName; 
                }
                string uid = shortUid(settingsPath);
                return $"{appName}-{uid}";
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
        public int MaxAgeMinutes { get; set; }

        [CategoryAttribute("Source")]
        public string Basename { get; set; }

        [CategoryAttribute("Source")]
        public string Prefix { get; set; }

        [CategoryAttribute("Source")]
        public string Suffix { get; set; }

        [CategoryAttribute("Source")]
        public string Webdate { get; set; }
        [CategoryAttribute("Source")]
        public string Minutepattern { get; set; }

        [CategoryAttribute("Source")]
        public string Hourpattern { get; set; }

        [CategoryAttribute("Target")]
        public string Discdate { get; set; }

        [CategoryAttribute("Target")]
        public bool UseLocaltime { get; set; }

        [CategoryAttribute("Target")]
        public int BroadcastMinuteAfter { get; set; }

        [CategoryAttribute("Target")]
        public int RetryIntervalSeconds { get; set; }

        [CategoryAttribute("Target")]
        public bool SafePublishing { get; set; }

        private string publishFolder;
        [CategoryAttribute("Target")]
        public string PublishFolder
        {
            get
            {
                return addDirectorSeparatorIfNeeded(publishFolder);
            }

            set
            {
                publishFolder = value;
            }
        }

        [CategoryAttribute("Source")]
        public string PublishName { get; set; }

        [CategoryAttribute("Target")]
        public string PublishFormat { get; set; }

        private bool updateAllEditions;
        [CategoryAttribute("Target")]
        public bool UpdateAllEditions
        {
            get
            {
                return updateAllEditions;
            }

            set
            {
                updateAllEditions = value;
            }
        }


        public int ValueWidth()
        {
            int w = 0;
            PropertyInfo[] p = this.GetType().GetProperties();
            for (int i = 0; i < p.Length; i++)
            {
                Object o = p[i].GetValue(this);
                if (o != null)
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

        public AppSettings ShallowCopy()
        {
            return (AppSettings)this.MemberwiseClone();
        }

        private string settingsLocation() {
            return Path.Combine(settingsPath, $"{appName}.config");
        }

        public bool LoadAppSettings()
        {
            XmlSerializer sz = null;
            FileStream fs = null;
            bool fileExists = false;
            try
            {
                sz = new XmlSerializer(typeof(AppSettings));
                string path = settingsLocation();
                FileInfo fi = new FileInfo(path);
                if (fi.Exists)
                {
                    fs = fi.OpenRead();
                    try
                    {
                        AppSettings mas = (AppSettings)sz.Deserialize(fs);
                        PropertyInfo[] p = this.GetType().GetProperties();
                        for (int i = 0; i < p.Length; i++) // or use ShallowCopy?
                        {
                            if(p[i].CanWrite) {
                                p[i].SetValue(this, p[i].GetValue(mas));
                            }
                        }
                        fileExists = true;
                    }
                    catch (Exception ex)
                    {
                        terseMessage(ex.Message);
                        // there was something wrong with the file. Delete and start again
                        fileExists = false;
                        fi.Delete();
                    }
                }
                if (!fileExists)
                {
                    setDefaults();
                    SaveAppSettings();
                }
            }
            catch (Exception ex)
            {
                terseMessage(ex.Message);
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

        private void setDefaults()
        {
            Logfolder = settingsPath;
            Archive = settingsPath;

            MinutesBefore = 4;
            MaxAgeMinutes = 10;
            Prefix = "";
            Basename = "";
            Webdate = "yyMMddHHmm";
            PublishFormat = "mp3";

            Hourpattern = "*";
            Minutepattern = "00,30";

            PublishFolder = @"C:\source\";
            PublishName = "audio";
            //Discdate = "HHmm";
            Discdate = "";
            BroadcastMinuteAfter = 0;
            RetryIntervalSeconds = 60;
            SafePublishing = true;

            PostLogs = true;
            LogUrl = "";
            City = "";
            Station = "";

            RunAsService = false;
            RunInForeground = false;
            appSettingsChanged = true;
        }

        public bool SaveAppSettings()
        {
            if (this.appSettingsChanged)
            {
                XmlSerializer sz = new XmlSerializer(typeof(AppSettings));
                StreamWriter sw = new StreamWriter(settingsLocation(), false);
                if (sw != null)
                {
                    sz.Serialize(sw, this);
                    sw.Close();
                }
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

        public string dateTimeToString(string format, DateTime t)
        {
            if (format == "")
                return "";
            if (format == "$") // custom format for unix timestamp
            {
                DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                TimeSpan o = t.Subtract(epoch);
                return o.TotalSeconds.ToString();
            }
            return t.ToString(format);
        }

        private string shortUid(string text)
        {
            return System.Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(
                    text.GetHashCode().ToString()
                )
            );
        }
    }
}
