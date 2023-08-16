using MCollector.Core;
using MCollector.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using MCollector.Core.Contracts;
using MCollector.Core.Config;

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

        public CollectorController(ICollectedDataPool dataPool, ICollectorSignal collectorSignal, IServiceProvider serviceProvider)
        {
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
        public IActionResult Status()
        {
            if(_config.Api?.Status == true)
            {
                var items = _dataPool.GetData().Select(d => new CollectedResult()
                {
                    Duration = d.Duration,
                    IsSuccess = d.IsSuccess,
                    Msg = !d.IsSuccess || _config.Api?.StatusContainsSuccessDetails == true ? (d.Content ?? "") : "",
                    LastUpdateTime = d.LastCollectTime,
                    Name = d.Name
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
    }
}
