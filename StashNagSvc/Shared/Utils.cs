using System;
using System.Configuration;

namespace StashNag.Shared
{
    public class Utils
    {
        public static TimeSpan GetWakeSpan(string monitorName)
        {
            TimeSpan monitorWakeSpan;
            try
            {
                monitorWakeSpan = TimeSpan.Parse(ConfigurationManager.AppSettings[$"{monitorName}WakeSpan"]);
            }
            catch
            {
                monitorWakeSpan = TimeSpan.Parse(ConfigurationManager.AppSettings["defaultWakeSpan"]);
            }

            return monitorWakeSpan;
        }
    }
}
