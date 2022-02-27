using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AspNetCoreSerilogExample.Web.Controllers;

// NOTE: TestController is the same for MS Logger as well as Serilog by design.
// You can use exactly the same logging for both and only difference is how you configure the 2 loggers.
[Route("api/[controller]/[action]")]
[ApiController]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;

    public TestController(ILogger<TestController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public void FlatLog(string input)
    {
        // NOTE: Don't use flat logs! Use structured logs. (example below)
        _logger.LogInformation($"Input text: {input}");
    }

    [HttpGet]
    public void StructuredLog(string input)
    {
        _logger.LogInformation("Input text: {Input}", input);
    }

    [HttpGet]
    public void EnrichLog(string input, string inputType)
    {
        _logger.LogInformation("Input text: {Input} with {InputType}", input, inputType);
    }

    [HttpGet]
    public void ScopeLog(string input, string additionalContext)
    {
        // Variable _ is discarded and this scope lasts until the end of the method.
        // You can still use using with curly brackets as before if you need it to limit scope or readability preference.
        // There a few utils that can simplify the code: https://github.com/jernejk/EfCoreSamples.Logging/blob/master/EfCoreSamples.Logging.Persistence/Utils/LoggingExtensions.cs
        using var _ = _logger.BeginScope(new Dictionary<string, object> { { "AdditionalContext", additionalContext } });

        _logger.LogInformation("Input text with scope: {Input}", input);

        MoreWork();
    }

    [HttpGet]
    public async Task TimeLog()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        await Task.Delay(new Random().Next(2000));
        stopwatch.Stop();

        _logger.LogInformation("Long task: {LongTimeElapsedMs}ms", stopwatch.ElapsedMilliseconds);
    }

    private void MoreWork()
    {
        // This will have additional properties in the structured log if called inside a log context.
        _logger.LogInformation("More logs");
    }
}
