using System;
using Microsoft.AspNetCore.Mvc;

namespace Api2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthCheckController : Controller
    {
        [HttpGet]
        public DateTimeOffset Get() => DateTimeOffset.UtcNow;
    }
}