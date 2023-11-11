using System;
using System.Threading.Tasks;
using DZarsky.CommonLibraries.AzureFunctions.Extensions;
using DZarsky.NotesFunction.Services;
using DZarsky.NotesFunction.Services.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace DZarsky.NotesFunction.HealthChecks;

public sealed class IntegrationConnectivityHealthCheck
{
    private readonly NoteService _noteService;
    private readonly string _testUserId;
    private readonly ILogger<IntegrationConnectivityHealthCheck> _logger;

    public IntegrationConnectivityHealthCheck(NoteService noteService, IConfiguration configuration,
        ILogger<IntegrationConnectivityHealthCheck> logger)
    {
        _noteService = noteService;
        _testUserId = configuration.GetValueFromContainer<string>("CosmosDB.HealthCheckUserID");
        _logger = logger;
    }

    public async Task<HealthCheckResult> PerformCheck()
    {
        try
        {
            var result = await _noteService.List(_testUserId);

            if (result.Status != ResultStatus.Success)
            {
                return HealthCheckResult.Degraded("Failed to retrieve notes");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to the service");
            
            return HealthCheckResult.Unhealthy("Failed to connect to the service", ex);
        }
        
        return HealthCheckResult.Healthy();
    }
}
