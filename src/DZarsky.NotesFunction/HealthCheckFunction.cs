using System.Threading.Tasks;
using DZarsky.NotesFunction.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DZarsky.NotesFunction;

public sealed class HealthCheckFunction
{
    private readonly IntegrationConnectivityHealthCheck _healthCheck;

    public HealthCheckFunction(IntegrationConnectivityHealthCheck healthCheck) => _healthCheck = healthCheck;

    [FunctionName("HealthCheck")]
    public async Task<IActionResult> RunCheck(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "healthz")] HttpRequest req)
    {
        var result = await _healthCheck.PerformCheck();

        return result.Status switch
        {
            HealthStatus.Healthy => new StatusCodeResult(StatusCodes.Status204NoContent),
            HealthStatus.Unhealthy => new StatusCodeResult(StatusCodes.Status503ServiceUnavailable),
            HealthStatus.Degraded => new StatusCodeResult(StatusCodes.Status503ServiceUnavailable),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };
    }
}
