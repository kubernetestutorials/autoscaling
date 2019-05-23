using Microsoft.AspNetCore.Mvc;

namespace CustomMetricsSample.Controllers
{
    [Route("/")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Base()
        {
            return "Hello World!";
        }

        [Route("/get")]
        [HttpGet]
        public ActionResult<double> Get()
        {
            return Counters.Counters.RequestsCounter.Value;
        }

        [Route("/add")]
        [HttpGet]
        public ActionResult<double> Add()
        {
            Counters.Counters.RequestsCounter.Inc();
            return Counters.Counters.RequestsCounter.Value;
        }

        [Route("/remove")]
        [HttpGet]
        public ActionResult<double> Remove()
        {
            Counters.Counters.RequestsCounter.Dec();
            return Counters.Counters.RequestsCounter.Value;
        }

        [Route("/set/{value}")]
        [HttpGet]
        public ActionResult<double> Set(int value)
        {
            Counters.Counters.RequestsCounter.Set(value);
            return Counters.Counters.RequestsCounter.Value;
        }
    }
}
