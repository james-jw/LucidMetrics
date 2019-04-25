using System.Collections.Generic;

namespace LucidMetrics
{
    public class LucidHostMetrics
    {
        public string Hostname { get; set; }
        public List<LucidMetric> Metrics { get; set; } = new List<LucidMetric>();
    }
}
