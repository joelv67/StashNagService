using CompassLogger;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using ServiceStack;
using System.Net;

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

        internal static void SendEmail(List<PullRequest> pullRequests)
        {
            string SmtpServer = ConfigurationManager.AppSettings["SmtpServer"];
            string FromEmailAddress = ConfigurationManager.AppSettings["FromEmailAddress"];
            string ToEmailAddress = ConfigurationManager.AppSettings["ToEmailAddress"];
            string subject = "Stale Pull Requests Need Action";
            string nagTime = TimeSpan.Parse(ConfigurationManager.AppSettings["NagTimeSpan"]).TotalDays.ToString();

            if (Environment.UserInteractive)
            {
                subject = $"Debug - {subject}";
            }

            List<string> emailErrorMessages = new List<string>();
            List<string> errorMessages = new List<string>();
            int errorCount = 0;
            foreach (PullRequest pullRequest in pullRequests)
            {
                string errorBackgroundColor = errorCount % 2 == 0 ? "FFFFFF" : "EEEEEE";
                StringBuilder emailErrorMessage = new StringBuilder();
                errorMessages.Add($"Repository:{pullRequest.Repository}, Project:{pullRequest.Project}, PR:{pullRequest.PRUrl}, LastActiveDate: {pullRequest.LastActiveDate}");

                emailErrorMessage.Append($"<div style='background-color:#{errorBackgroundColor};border:1px dotted gray; margin-bottom: .25em; ppadding:.25em'>");
                emailErrorMessage.Append($"<p style='margin: .25em'><strong>{pullRequest.Repository} - {pullRequest.Project}<br></strong></p>");
                emailErrorMessage.Append($"<p style='margin: .25em'><a href='{pullRequest.PRUrl}'>{pullRequest.PRUrl}</a>");
                emailErrorMessage.Append($"<br>Last Active: {pullRequest.LastActiveDate}</p>");
                emailErrorMessage.Append($"</div>");
                emailErrorMessages.Add(emailErrorMessage.ToString());

                errorCount++;
            }

            Logger.Error($"The following {pullRequests.Count} Pull Requests were found to be stale:\n{string.Join(Environment.NewLine, errorMessages)}");

            string body = "<div style='margin: 0;font-size:14px;line-height:17px;color:#1F497D;font-family:Arial, Helvetica, sans-serif'>" +
                            $"<p style='margin: 0;font-size: 22px;line-height: 26px'><strong>{subject}</strong></p>" +
                            $"<p style='margin: 0;'>The following {pullRequests.Count} Pull Requests haven't had activity for at least {nagTime} days<br><br></p>" +
                            string.Join("", emailErrorMessages) + "</div>";

            MailMessage mail = new MailMessage()
            {
                From = new MailAddress(FromEmailAddress),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(body, null, MediaTypeNames.Text.Html));
            mail.To.Add(new MailAddress(ToEmailAddress));

            try
            {
                using (SmtpClient smpt = new SmtpClient())
                {
                    smpt.Host = SmtpServer;
                    smpt.Timeout = 15000;
                    smpt.Send(mail);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Unable to send mail. {e.ToString()}");
            }
        }

    }
}
