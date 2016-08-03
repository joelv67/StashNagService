using System;
using System.ServiceProcess;

namespace StashNag
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            StashNagSvcService stashNagService = new StashNagSvcService(args);
            if (Environment.UserInteractive)
            {
                stashNagService.TestStartupAndStop(args);
            }
            else
            {
                ServiceBase.Run(stashNagService);
            }
        }
    }
}
