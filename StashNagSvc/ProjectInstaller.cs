using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;

namespace StashNag
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        private const string LogName = "Stash Nag Log";
        private const string LogSource = "StashNag";

        public ProjectInstaller()
        {
            InitializeComponent();
            EventLogInstaller EventLogInstall = null;
            foreach (Installer I in this.serviceInstaller1.Installers)
            {
                EventLogInstall = I as EventLogInstaller;
                if (EventLogInstall != null)
                {
                    EventLogInstall.Log = LogName;
                    EventLogInstall.Source = LogSource;
                    EventLogInstall.UninstallAction = UninstallAction.NoAction;
                }
            }
        }

        private void serviceInstaller1_AfterInstall(object sender, InstallEventArgs e)
        {
            if (!EventLog.SourceExists(LogSource))
            {
                EventSourceCreationData creationData = new
                EventSourceCreationData(LogSource, LogName);
                creationData.MachineName = Environment.MachineName;
                EventLog.CreateEventSource(creationData);
            }
            EventLog el = new EventLog(LogName, Environment.MachineName, LogSource);
            el.MaximumKilobytes = 1024000; // 1 gig
            el.WriteEntry("Service installed.");
        }
    }
}
