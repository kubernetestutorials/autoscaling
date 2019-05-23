using Prometheus;

namespace CustomMetricsSample.Counters
{
    public static class Counters
    {
        public static readonly Gauge RequestsCounter = Metrics.CreateGauge("my_app_num_requests", "Number of requests.");
    }
}
