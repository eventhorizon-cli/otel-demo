using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;

[Route("/api/[controller]")]
public class BarController : ControllerBase
{
    private readonly ILogger<BarController> _logger;

    public BarController(ILogger<BarController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<string> Get()
    {
        _logger.LogInformation("/api/bar called");

        var baggage1 = Baggage.GetBaggage("FooBaggage1");
        var baggage2 = Baggage.GetBaggage("FooBaggage2");
        
        _logger.LogInformation($"FooBaggage1: {baggage1}, FooBaggage2: {baggage2}");

        return "Hello from Bar";
    }
}