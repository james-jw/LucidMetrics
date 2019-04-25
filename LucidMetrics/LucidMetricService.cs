using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using System.Configuration;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;

namespace LucidMetrics
{
    public partial class LucidMetricService : ServiceBase
    {
        private int _interval;
        private Uri _collectorUri;
        private Timer _timer = new Timer();
        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _memoryCounter;
        private string _hostIdentity;

        public LucidMetricService()
        {
            InitializeComponent();

            _hostIdentity = ConfigurationManager.AppSettings["hostIdentity"]?.ToString();

            if (_hostIdentity == null)
                throw new ArgumentNullException(nameof(_hostIdentity), "No host identity specified in appsettings");

            var uriString = ConfigurationManager.AppSettings["metricCollectorUri"]?.ToString();

            if (uriString == null)
                throw new ArgumentNullException(nameof(_collectorUri), "No collector uri specified in appsettings");

            _collectorUri = new Uri(uriString);

            
            if(!int.TryParse(ConfigurationManager.AppSettings["interval"], out _interval))
            {
                _interval = 5000;
            }

        }

        protected override void OnStart(string[] args)
        {
            _timer.Elapsed += _timer_Elapsed;
            _timer.Interval = 5000;
            _timer.Enabled = true;

            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _memoryCounter = new PerformanceCounter("Memory", "Available MBytes"); 

        }

        /// <summary>
        /// Callback to push current metrics 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var metrics = new LucidHostMetrics()
            {
                Hostname = Dns.GetHostName(),
                Metrics = new System.Collections.Generic.List<LucidMetric>()
                {
                    new LucidMetric()
                    {
                        Name = "Processor Usage",
                        Value = _cpuCounter.NextValue()
                    },
                    new LucidMetric()
                    {
                        Name = "Memory Available (MB)",
                        Value = _memoryCounter.NextValue()
                    }
                }
            };

            var metricContent = JsonConvert.SerializeObject(metrics);
            using (var client = new HttpClient())
            {
                await client.PostAsync($"{_collectorUri}/api/v1/metrics", new StringContent(metricContent, Encoding.UTF32, "application/json"));
            }
        }

        protected override void OnStop()
        {
            _timer.Enabled = false;
            _timer?.Dispose();
        }
    }
}
