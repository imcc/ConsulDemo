using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Consul;
using Core.Options;
using DnsClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : Controller
    {
        private readonly IDnsQuery _dnsQuery;
        private readonly IConsulClient _client;
        private readonly ServiceDiscoveryOptions _options;

        private const string ConsulDomain = "service.consul";

        public HomeController(IConsulClient client, IOptions<ServiceDiscoveryOptions> options, IDnsQuery dnsQuery)
        {
            _client = client;
            _dnsQuery = dnsQuery;
            _options = options.Value;
        }

        /// <summary>
        /// 通过 ConsulClient 获取 Url，能获取到所有，需要手动负载均衡
        /// </summary>
        /// <returns></returns>
        public string Get()
        {
            // 获取所有注册的地址
            var services = _client.Agent.Services().GetAwaiter().GetResult().Response
                .Where(x => x.Value.Service == _options.ServiceName);

            var addresses = new List<string>();
            foreach (var (_, value) in services)
            {
                addresses.Add($"{value.Address}:{value.Port}");
            }

            // 取到了所有的地址，自己做负载均衡
            return string.Join(',', addresses);
        }

        /// <summary>
        /// 通过 DnsClient 获取 Url，自动负载均衡
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Dns")]
        public async Task<string> DnsClient()
        {
            var result = await _dnsQuery.ResolveServiceAsync(ConsulDomain, _options.ServiceName);
            var firstResult = result.FirstOrDefault();
            if (firstResult == null) return string.Empty;

            // 每次只取第一个就行，自动负载均衡
            var address = firstResult.AddressList.FirstOrDefault()?.ToString();
            if (string.IsNullOrWhiteSpace(address))
            {
                address = firstResult.HostName;
            }

            var port = firstResult.Port;
            return $"{address}:{port}";
        }
    }
}