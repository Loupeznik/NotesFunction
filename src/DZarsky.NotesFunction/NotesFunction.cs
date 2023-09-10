using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DZarsky.CommonLibraries.AzureFunctions.General;
using DZarsky.CommonLibraries.AzureFunctions.Models.Auth;
using DZarsky.NotesFunction.General;
using DZarsky.NotesFunction.Models;
using DZarsky.NotesFunction.Services;
using DZarsky.NotesFunction.Services.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using DZarsky.CommonLibraries.AzureFunctions.Security;

namespace DZarsky.NotesFunction
{
    public class NotesFunction
    {
        private readonly NoteService _noteService;
        private readonly IAuthManager _authManager;

        public NotesFunction( NoteService noteService, IAuthManager authManager)
        {
            _noteService = noteService;
            _authManager = authManager;
        }

        [FunctionName(nameof(CreateNote))]
        [OpenApiOperation(operationId: nameof(CreateNote), tags: new[] { Constants.NotesSectionName })]
        [OpenApiSecurity(ApiConstants.BasicAuthSchemeID, SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Basic)]
        [OpenApiRequestBody(ApiConstants.JsonContentType, typeof(NoteDto))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: ApiConstants.JsonContentType, bodyType: typeof(NoteDto), Description = "The OK response")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = nameof(HttpStatusCode.BadRequest))]
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
        [OpenApiSecurity(ApiConstants.BasicAuthSchemeID, SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Basic)]
        [OpenApiRequestBody(ApiConstants.JsonContentType, typeof(NoteDto))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: ApiConstants.JsonContentType, bodyType: typeof(NoteDto), Description = "Success")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = nameof(HttpStatusCode.BadRequest))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = nameof(HttpStatusCode.NotFound))]
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
        [OpenApiSecurity(ApiConstants.BasicAuthSchemeID, SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Basic)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: ApiConstants.JsonContentType, bodyType: typeof(NoteDto), Description = "Success")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = nameof(HttpStatusCode.BadRequest))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = nameof(HttpStatusCode.NotFound))]
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
        [OpenApiParameter("getDeleted", In = ParameterLocation.Query, Type = typeof(bool), Required = false, Description = "Determines if deleted records should be fetched, defaults to 'false'")]
        [OpenApiSecurity(ApiConstants.BasicAuthSchemeID, SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Basic)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: ApiConstants.JsonContentType, bodyType: typeof(List<NoteDto>), Description = "The OK response")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = nameof(HttpStatusCode.BadRequest))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = nameof(HttpStatusCode.NotFound))]
        public async Task<IActionResult> GetNotes(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Constants.NotesSectionName)] HttpRequest req)
        {
            var authResult = await Authorize(req);

            if (authResult.Status != AuthResultStatus.Success)
            {
                return new UnauthorizedResult();
            }

            var getDeleted = req.Query.ContainsKey("getDeleted") && bool.TryParse(req.Query["getDeleted"], out var getDeletedValue) && getDeletedValue;

            var result = await _noteService.List(authResult.UserID, getDeleted);

            return ResolveResultStatus(result.Status, result.Result);
        }

        [FunctionName(nameof(DeleteNote))]
        [OpenApiOperation(operationId: nameof(DeleteNote), tags: new[] { Constants.NotesSectionName })]
        [OpenApiSecurity(ApiConstants.BasicAuthSchemeID, SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Basic)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: ApiConstants.JsonContentType, bodyType: typeof(object), Description = "Success")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = nameof(HttpStatusCode.BadRequest))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = nameof(HttpStatusCode.NotFound))]
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

        private async Task<AuthResult> Authorize(HttpRequest request) => await _authManager.ValidateToken(request.Headers.Authorization);

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
