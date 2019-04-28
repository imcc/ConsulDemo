using System;
using System.Diagnostics;
using System.Linq;
using Consul;
using Core.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Builders
{
    public static class RegisterWithConsul
    {
        public static IApplicationBuilder UseConsul(this IApplicationBuilder app
            , IApplicationLifetime lifetime)
        {
            var client = app.ApplicationServices.GetRequiredService<IConsulClient>();
            var options = app.ApplicationServices.GetRequiredService<IOptions<ServiceDiscoveryOptions>>().Value;
            var logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger<IApplicationBuilder>();

            // Server 是一个HTTP服务器，负责HTTP的监听。
            // 接收一组 FeatureCollection 类型的原始请求，并将其包装成 HttpContext 以供我们的应用程序完成响应的处理。
            // 前面注册的 Kestrel 便是默认的 Server
            var features = app.Properties["server.Features"] as FeatureCollection;
            Debug.Assert(features != null, nameof(features) + " != null");
            var addresses = features.Get<IServerAddressesFeature>().Addresses.Select(p => new Uri(p));
            foreach (var address in addresses)
            {
                var serviceId = $"{options.ServiceName}_{address.Host}:{address.Port}";
                var httpCheck = new AgentServiceCheck
                {
                    DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1),
                    Interval = TimeSpan.FromSeconds(30),
                    HTTP = new Uri(address, options.HealthCheck).OriginalString
                };

                var host = $"{address.Scheme}://{address.Host}";
                var registration = new AgentServiceRegistration
                {
                    ID = serviceId,
                    Address = host,
                    Port = address.Port,
                    Name = options.ServiceName,
                    Checks = new[] {httpCheck}
                };

                if (options.Tags != null && options.Tags.Length > 0)
                {
                    registration.Tags = options.Tags;
                }

                // 应用启动时向 Consul 绑定
                logger.LogDebug($"开始注册 Consul: " + address.OriginalString);
                client.Agent.ServiceRegister(registration).GetAwaiter().GetResult();

                // 应用停止时向 Consul 解绑
                lifetime.ApplicationStopping.Register(() =>
                {
                    logger.LogDebug("解除注册 Consul");
                    client.Agent.ServiceDeregister(serviceId).GetAwaiter().GetResult();
                });
            }

            return app;
        }
    }
}