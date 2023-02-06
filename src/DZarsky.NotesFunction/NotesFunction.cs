using System.IO;
using System.Net;
using System.Threading.Tasks;
using DZarsky.CommonLibraries.AzureFunctions.General;
using DZarsky.CommonLibraries.AzureFunctions.Models.Auth;
using DZarsky.CommonLibraries.AzureFunctions.Security.CosmosDB;
using DZarsky.NotesFunction.General;
using DZarsky.NotesFunction.Models;
using DZarsky.NotesFunction.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace DZarsky.NotesFunction
{
    public class NotesFunction
    {
        private readonly CosmosAuthManager _authManager;
        private readonly NoteService _noteService;

        public NotesFunction(CosmosAuthManager authManager, NoteService noteService)
        {
            _authManager = authManager;
            _noteService = noteService;
        }

        [FunctionName(nameof(CreateNote))]
        [OpenApiOperation(operationId: nameof(CreateNote), tags: new[] { Constants.NotesSectionName })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: ApiConstants.JsonContentType, bodyType: typeof(NoteDto), Description = "The OK response")]
        public async Task<IActionResult> CreateNote(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = Constants.NotesSectionName)] NoteDto note, HttpRequest req)
        {
            var authResult = await Authorize(req);

            if (authResult.Status != AuthResultStatus.Success)
            {
                return new UnauthorizedResult();
            }

            var result = await _noteService.Create(note, authResult.UserID);

            if (result.Status == Services.Models.ResultStatus.BadRequest)
            {
                return new BadRequestResult();
            }

            return new OkObjectResult(result.Result);
        }

        private async Task<AuthResult> Authorize(HttpRequest request)
        {
            var authHeader = request.Headers["Authorization"];

            var credentials = CosmosAuthManager.ParseToken(authHeader);

            if (credentials == null)
            {
                return new AuthResult(AuthResultStatus.InvalidLoginOrPassword);
            }

            return await _authManager.ValidateCredentials(credentials);
        }
    }
}
