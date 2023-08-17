using MCollector.Core;
using MCollector.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using MCollector.Core.Contracts;
using MCollector.Core.Config;
using Microsoft.AspNetCore.DataProtection;
using MCollector.Core.Common;

namespace MCollector.Controllers
{
    [ApiController]
    [Produces("application/json")]
    [Route("/")]
    public class CollectorController: ControllerBase
    {
        ICollectedDataPool _dataPool;
        ICollectorSignal _collectorSignal;
        CollectorConfig _config;
        IProtector _protector;


        public CollectorController(ICollectedDataPool dataPool, ICollectorSignal collectorSignal, IServiceProvider serviceProvider, IProtector protector)
        {
            _protector = protector;
            _dataPool = dataPool;
            _collectorSignal = collectorSignal;
            _config = serviceProvider.GetRequiredService<IOptions<CollectorConfig>>().Value;
        }

        [Route("/health")]
        public IActionResult Health()
        {
            return Ok();
        }

        [Route("/status")]
        [HttpGet]
        public IActionResult Status(IDataProtectionProvider provider)
        {
            if(_config.Api?.Status == true)
            {
                var items = _dataPool.GetData().Select(d => new CollectedResult()
                {
                    Duration = d.Duration,
                    IsSuccess = d.IsSuccess,
                    Msg = !d.IsSuccess || _config.Api?.StatusContainsSuccessDetails == true ? (d.Content ?? "") : "",
                    LastUpdateTime = d.LastCollectTime,
                    Name = d.Name,
                    Remark = d.Remark
                });//

                return Ok(items);
            }

            return NotFound("404");
        }

        [Route("/refresh")]
        public IActionResult Refresh()
        {
            //todo

            if (_config.Api?.Refresh == true)
            {
                _collectorSignal.Continue();

                return Ok("ok");
            }

            return NotFound(null);
        }

        [Route("/encrypt")]
        [Produces("text/plain")]
        [HttpGet]
        public IActionResult Encrypt(string content)
        {
            var txt = _protector.Protect(content);

            return Ok(txt);
        }
    }
}
