namespace Core.Options
{
    public class ServiceDiscoveryOptions
    {
        public string ServiceName { get; set; }

        public string HealthCheck { get; set; } = "HealthCheck";

        public string[] Tags { get; set; }

        public ConsulOptions Consul { get; set; }
    }
}