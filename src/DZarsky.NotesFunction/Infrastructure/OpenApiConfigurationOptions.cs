using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using System.Collections.Generic;

namespace DZarsky.NotesFunction.Infrastructure
{
    public class OpenApiConfigurationOptions : IOpenApiConfigurationOptions
    {
        public OpenApiInfo Info { get; set; } = new OpenApiInfo()
        {
            Version = "1.0.0",
            Title = "NotesFunction",
            Description = "A Azure Function for CRUD operations on notes stored in CosmosDB",
        };

        public OpenApiVersionType OpenApiVersion { get; set; } = OpenApiVersionType.V2;

        public bool IncludeRequestingHostName { get; set; } = false;

        public bool ForceHttp { get; set; } = false;

        public bool ForceHttps { get; set; } = false;
        public List<OpenApiServer> Servers { get; set; } = new List<OpenApiServer>();
    }
}
