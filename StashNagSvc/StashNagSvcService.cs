using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Configuration;
using StashNag.Shared;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using CompassLogger;

namespace StashNag
{
    public partial class StashNagSvcService : ServiceBase
    {
        public static string LogName = ConfigurationManager.AppSettings["AppLogName"];
        public static string LogSource = ConfigurationManager.AppSettings["AppLogSource"];
        List<Task> nagTasks;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly CancellationToken cancellationToken;

        /// <summary>
        /// List of imported task engines
        /// </summary>
        [ImportMany(typeof(ITaskEngine))]
        private IEnumerable<Lazy<ITaskEngine, INameMetadata>> taskEngines;
        private bool onStopTriggered = false;

        public StashNagSvcService(string[] args)
        {
            Logger.Info("Service Initializing...");
            InitializeComponent();
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            AggregateCatalog aggregateCatalog = new AggregateCatalog();
            aggregateCatalog.Catalogs.Add(new AssemblyCatalog(Assembly.GetEntryAssembly()));
            CompositionContainer container = new CompositionContainer(aggregateCatalog);
            container.SatisfyImportsOnce(this);

            Logger.Info($"Service Initialized.");
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                Logger.Info("Service Starting up...");

                List<string> stashUrls = GetStashServersToCheck();
                nagTasks = new List<Task>();

                foreach (var curUrl in stashUrls)
                {
                    if (onStopTriggered)
                    {
                        return;
                    }
                    else
                    {
                        Task task = Task.Factory.StartNew(() =>
                        {
                            processEngine(curUrl, taskEngines.First().Value, cancellationToken, (taskName) =>
                            {
                                executeMonitor(taskName, cancellationToken);
                            });
                        });
                        nagTasks.Add(task);
                    }
                }

                Logger.Info($"Service Started. running {stashUrls.Count} Stash URLs. List of Stash URLs: {string.Join(", ", stashUrls)}");
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
        }

        private List<string> GetStashServersToCheck()
        {
            return ConfigurationManager.AppSettings["StashUrls"].Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        private void executeMonitor(string taskName, CancellationToken processToken)
        {
            throw new NotImplementedException();
        }

        protected override void OnStop()
        {
            Logger.Info("Service Stopping...");
            cancellationTokenSource.Cancel();
            DateTime waitForCancelUntil = DateTime.Now.AddSeconds(5);
            while(nagTasks.Any(p => p.IsCompleted != true && DateTime.Now < waitForCancelUntil))
            {
                Logger.Info($"Waiting for {nagTasks.Where(p => p.IsCompleted != true).Count()} monitor to shutdown...");
                Thread.Sleep(1000);
            }
            Logger.Info("Service Stopped.");
        }

        internal void TestStartupAndStop(string[] args)
        {
            Console.WriteLine("########################\n# You are in debug mode\n########################\n");
            this.OnStart(args);
            Console.WriteLine("Press 'q' to stop program.");
            while (true)
            {
                ConsoleKeyInfo key = System.Console.ReadKey();
                if (key.Key == ConsoleKey.Q)
                    break;
            }
            this.OnStop();
        }

        protected virtual void processEngine(string stashURL, ITaskEngine engine, CancellationToken processToken, Action<string> functionToExecute)//, string engineToRun)
        {
            engine.executeStashNag(stashURL, processToken);
        }
    }
}
