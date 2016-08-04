using CompassLogger;
using StashNag.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Linq;
using System.Threading;

namespace StashNag
{
    [Export(typeof(ITaskEngine))]
    [ExportMetadata("Name", "StashNag")]
    public class StashNag : ITaskEngine
    {
        //private static int EventId = 1000;
        public static readonly string hipChatToken = ConfigurationManager.AppSettings["HipChatToken"];
        public static readonly string hipChatRoomName = ConfigurationManager.AppSettings["HipChatRoomName"];

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
                        DateTime today = DateTime.Now;
                        bool isHolidayOrWeekend = Utils.IsHoliday(today) || today.DayOfWeek == DayOfWeek.Sunday || today.DayOfWeek == DayOfWeek.Saturday;
                        if (isHolidayOrWeekend)
                        {
                            Logger.Info($"Today is Holiday or Weekend, Processing Complete.");
                            continue;
                        }

                        List<string> repoUrls = ConfigurationManager.AppSettings["StashUrls"].ToString().Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                        foreach (string repoUrl in repoUrls)
                        {
                            Logger.Info($"Checking {repoUrl}...");

                            // Begin Replace with Stash Checking Code
                            List<PullRequest> pullRequests = new List<PullRequest>();
                            pullRequests.Add(new PullRequest("eCIS Systems Automation", "Compass", "https://stash/projects/ESA/repos/compass/pull-requests/911/overview", DateTime.Now.AddDays(-5)));
                            pullRequests.Add(new PullRequest("eCIS Systems Automation", "Cats", "https://stash/projects/ESA/repos/cats/pull-requests/8/overview", DateTime.Now.AddDays(-6)));
                            // End Replace with Stash checking code

                            Utils.SendEmail(pullRequests);
                            Logger.Info($"Finished Checking {repoUrl}...");
                        }

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
    }

}