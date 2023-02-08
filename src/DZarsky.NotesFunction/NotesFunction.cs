using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DZarsky.CommonLibraries.AzureFunctions.General;
using DZarsky.CommonLibraries.AzureFunctions.Models.Auth;
using DZarsky.CommonLibraries.AzureFunctions.Security.CosmosDB;
using DZarsky.NotesFunction.General;
using DZarsky.NotesFunction.Models;
using DZarsky.NotesFunction.Services;
using DZarsky.NotesFunction.Services.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;

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

            return ResolveResultStatus(result.Status, result.Result);
        }

        [FunctionName(nameof(UpdateNote))]
        [OpenApiOperation(operationId: nameof(UpdateNote), tags: new[] { Constants.NotesSectionName })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: ApiConstants.JsonContentType, bodyType: typeof(NoteDto), Description = "The OK response")]
        public async Task<IActionResult> UpdateNote(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = Constants.NotesSectionName)] NoteDto note, HttpRequest req)
        {
            var authResult = await Authorize(req);

            if (authResult.Status != AuthResultStatus.Success)
            {
                return new UnauthorizedResult();
            }

            var result = await _noteService.Update(note, authResult.UserID);

            return ResolveResultStatus(result.Status, result.Result);
        }

        [FunctionName(nameof(GetNote))]
        [OpenApiOperation(operationId: nameof(GetNote), tags: new[] { Constants.NotesSectionName })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: ApiConstants.JsonContentType, bodyType: typeof(NoteDto), Description = "The OK response")]
        public async Task<IActionResult> GetNote(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Constants.NotesSectionName + "/{noteId}")] HttpRequest req, string? noteId)
        {
            if (string.IsNullOrWhiteSpace(noteId))
            {
                return new BadRequestResult();
            }

            var authResult = await Authorize(req);

            if (authResult.Status != AuthResultStatus.Success)
            {
                return new UnauthorizedResult();
            }

            var result = await _noteService.Get(noteId, authResult.UserID);

            return ResolveResultStatus(result.Status, result.Result);
        }

        [FunctionName(nameof(GetNotes))]
        [OpenApiOperation(operationId: nameof(GetNotes), tags: new[] { Constants.NotesSectionName })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: ApiConstants.JsonContentType, bodyType: typeof(List<NoteDto>), Description = "The OK response")]
        public async Task<IActionResult> GetNotes(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Constants.NotesSectionName)] HttpRequest req)
        {
            var authResult = await Authorize(req);

            if (authResult.Status != AuthResultStatus.Success)
            {
                return new UnauthorizedResult();
            }

            var result = await _noteService.List(authResult.UserID);

            return ResolveResultStatus(result.Status, result.Result);
        }

        [FunctionName(nameof(DeleteNote))]
        [OpenApiOperation(operationId: nameof(DeleteNote), tags: new[] { Constants.NotesSectionName })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: ApiConstants.JsonContentType, bodyType: typeof(List<NoteDto>), Description = "The OK response")]
        public async Task<IActionResult> DeleteNote(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = Constants.NotesSectionName + "/{noteId}")] HttpRequest req, string? noteId)
        {
            if (string.IsNullOrWhiteSpace(noteId))
            {
                return new BadRequestResult();
            }

            var authResult = await Authorize(req);

            if (authResult.Status != AuthResultStatus.Success)
            {
                return new UnauthorizedResult();
            }

            var result = await _noteService.Delete(noteId, authResult.UserID);

            return ResolveResultStatus<GenericResult>(result.Status, null);
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

        private static IActionResult ResolveResultStatus<T>(ResultStatus status, T? result)
        {
            if (result != null && status == ResultStatus.Success)
            {
                return new OkObjectResult(result);
            }

            return status switch
            {
                ResultStatus.Success => new OkResult(),
                ResultStatus.BadRequest => new BadRequestResult(),
                ResultStatus.Failed => new BadRequestResult(),
                ResultStatus.AlreadyExists => new ConflictResult(),
                ResultStatus.NotFound => new NotFoundResult(),
                _ => new BadRequestResult(),
            };
        }
    }
}
