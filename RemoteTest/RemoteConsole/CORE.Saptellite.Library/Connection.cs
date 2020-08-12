using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CORE.Saptellite.Library
{
    public static class Connection
    {

        public static string ConnectAddress
        {
            get
            {
                return GetValueFromConfig("ConnectAddress");

            }
        }

        public static int Port
        {
            get
            {
                return int.Parse(GetValueFromConfig("ConnectPort"));

            }
        }

        private static string GetValueFromConfig(string key)
        {
            string url = string.Empty;
            System.Configuration.Configuration config = null;
            string exeConfigPath = typeof(Connection).Assembly.Location;
            try
            {
                config = ConfigurationManager.OpenExeConfiguration(exeConfigPath);
            }
            catch (Exception ex)
            {
                throw new System.IO.FileNotFoundException("Couldn't locate the Connection.config file", ex);

            }

            if (config != null)
            {
                url = GetAppSetting(config, key);
            }
            return url;

        }

        private static string GetAppSetting(Configuration config, string key)
        {
            KeyValueConfigurationElement element = config.AppSettings.Settings[key];
            if (element != null)
            {
                string value = element.Value;
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
            return string.Empty;
        }
    }
}
