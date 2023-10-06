using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;

[Route("/api/[controller]")]
public class FooController : ControllerBase
{
    private static readonly ActivitySource FooActivitySource
        = new ActivitySource("FooSource");
    private static readonly Counter<int> FooCounter
        = new Meter("FooMeter").CreateCounter<int>("FooCounter");

    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<FooController> _logger;

    public FooController(
        IHttpClientFactory clientFactory,
        ILogger<FooController> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        _logger.LogInformation("/api/foo called");

        Baggage.SetBaggage("FooBaggage1", "FooValue1");
        Baggage.SetBaggage("FooBaggage2", "FooValue2");

        var client = _clientFactory.CreateClient();
        var result = await client.GetStringAsync("http://localhost:5069/api/bar");

        using var activity = FooActivitySource.StartActivity("FooActivity");
        activity?.AddTag("FooTag", "FooValue");
        activity?.AddEvent(new ActivityEvent("FooEvent"));
        await Task.Delay(100);

        FooCounter.Add(1);

        return Ok(result);
    }
}