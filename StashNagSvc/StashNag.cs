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

namespace StashNag
{
    [Export(typeof(ITaskEngine))]
    [ExportMetadata("Name", "StashNag")]
    public class StashNag : ITaskEngine
    {
        //private static int EventId = 1000;
        public static readonly string SmtpServer = ConfigurationManager.AppSettings["SmtpServer"];
        public static readonly string FromEmailAddress = ConfigurationManager.AppSettings["FromEmailAddress"];
        public static readonly string ToEmailAddress = ConfigurationManager.AppSettings["ToEmailAddress"];

        public void executeStashNag(string stashUrl, CancellationToken processToken)
        {
            try
            {
                //Logger.OverrideThreadContextEventId(EventId);
                Logger.OverrideThreadContextModuleProperty(stashUrl);
                TimeSpan wakeSpan = TimeSpan.Parse(ConfigurationManager.AppSettings["WakeSpan"]);

                while (!processToken.IsCancellationRequested)
                {
                    try
                    {
                        Logger.Info($"Processing started...");

                        //TODO: Get the Stash data here - stash API call and json data

                        //TODO: If StashCheck returned PR's to alert on, send report (email and/or hipchat)
                        
                        // ### Below is commented out as sample of writing data to log and email ###



                        //if (needToNag)
                        //{
                        //    List<string> errorMessages = new List<string>();
                        //    List<string> emailErrorMessages = new List<string>();

                        //    int errorCount = 0;

                        //    //foreach (DataRow row in ds.Tables[0].Rows)
                        //    //{
                        //    //    string message = row["Message"].ToString();
                        //    //    Regex regex = new Regex("not updated by '(?<agentModule>.*?)' as expected");
                        //    //    string agentModule = regex.Match(message).Groups["agentModule"].Value.ToUpper();

                        //    //    regex = new Regex("Domain:? ?'(?<Domain>.*?)'");
                        //    //    var regexMatch = regex.Match(message);
                        //    //    string DomainToReport = string.IsNullOrEmpty(regexMatch.Groups["Domain"].ToString()) ? row["Domain"].ToString().ToUpper() : regexMatch.Groups["Domain"].ToString().ToUpper();

                        //    //    regex = new Regex("Machine:? ?'(?<Machine>.*?)'");
                        //    //    regexMatch = regex.Match(message);
                        //    //    string MachineToReport = string.IsNullOrEmpty(regexMatch.Groups["Machine"].ToString()) ? row["Server"].ToString().ToUpper() : regexMatch.Groups["Machine"].ToString().ToUpper();

                        //    //    StringBuilder emailErrorMessage = new StringBuilder();
                        //    //    string extraErrorMsg = string.Empty;

                        //    //    string errorBackgroundColor = errorCount % 2 == 0 ? "FFFFFF" : "EEEEEE";
                        //    //    errorMessages.Add($"{row["ModuleTested"]}: {row["Domain"]}: {row["Server"]}:\n{message}\n{extraErrorMsg}");
                        //    //    emailErrorMessage.Append($"<div style='background-color:#{errorBackgroundColor};border:1px dotted gray; margin-bottom: .25em; ppadding:.25em'>");
                        //    //    emailErrorMessage.Append($"<p style='margin: .25em'><strong>{row["ModuleTested"]} - {DomainToReport} - {MachineToReport}<br></strong></p>");
                        //    //    emailErrorMessage.Append($"<p style='margin: .25em'>{row["starttime"]} - {message}");
                        //    //    if (!string.IsNullOrEmpty(extraErrorMsg))
                        //    //    {
                        //    //        emailErrorMessage.Append("<br>{extraErrorMsg}");
                        //    //    }
                        //    //    emailErrorMessage.Append($"<br>Reported By: {row["ModuleTested"]} - {row["Domain"]} - {row["Server"]}</p>");
                        //    //    emailErrorMessage.Append($"</div>");
                        //    //    emailErrorMessages.Add(emailErrorMessage.ToString());
                        //    //    errorCount++;

                        //    //}
                        //    //Logger.Error($"The following {errorCount} error messages were found for Compass HealthMonitor:\n{string.Join(Environment.NewLine, errorMessages)}");
                        //    //SendEmail("Compass Health Monitor Errors", emailErrorMessages, monitorWakeSpan.TotalHours, $"{startingTime.Value} - {endingTime.Value}", errorCount);
                        //}
                        //else
                        //{
                        //    Logger.Info($"No errors found for StartTime: {startingTime.Value} - {endingTime.Value}");
                        //}
                        Logger.Info("Processing complete...");
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.ToString());
                    }

                    if (processToken.IsCancellationRequested)
                        return;

                    //wait for next time to run this monitor
                    Logger.Info($"Sleeping until {DateTime.Now.Add(wakeSpan)} with wake span of {wakeSpan.ToString()}");
                    Thread.Sleep(wakeSpan);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to initialize! {e.ToString()}");
            }
        }

        private void SendEmail(string subject, List<string> emailErrorMessages, double hours, string timeSpan, int errorCount)
        {
           if (Environment.UserInteractive)
            {
                subject = $"Debug - {subject}";
            }

            string body = "<div style='margin: 0;font-size:14px;line-height:17px;color:#1F497D;font-family:Arial, Helvetica, sans-serif'>" +
                            $"<p style='margin: 0;font-size: 22px;line-height: 26px'><strong>{subject}</strong></p>" +
                            $"<p style='margin: 0;'>The following {errorCount} errors were found for the {hours} hour time span: {timeSpan}<br><br></p>" +
                            string.Join("",emailErrorMessages) + "</div>";

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
            catch(Exception e)
            {
                Logger.Error($"Unable to send mail. {e.ToString()}");
            }
        }
    }
}
