using CompassLogger;
using StashNag.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using HipchatApiV2;
using ServiceStack;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Threading.Tasks;

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

        public static bool IsHoliday(DateTime date)
        {
            DateTime dateToCompare = date.Date;

            //ExpediaHolidayURL
            string holidayURL = ConfigurationManager.AppSettings["HolidayURL"];
            if(string.IsNullOrEmpty(holidayURL))
            {
                return false;
            }

            HttpWebRequest HttpWReq = (HttpWebRequest)WebRequest.Create($"{holidayURL}&year={dateToCompare.Year}&month={dateToCompare.Month}&day={dateToCompare.Day}");
            HttpWebResponse response = (HttpWebResponse)HttpWReq.GetResponse();

            string responseAsText = Encoding.Default.GetString(response.GetResponseStream().ReadFully());
            return responseAsText != "{\"status\":200,\"holidays\":[]}";
        }

    }
}
