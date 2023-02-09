using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DZarsky.CommonLibraries.AzureFunctions.Extensions;
using DZarsky.CommonLibraries.AzureFunctions.General;
using DZarsky.CommonLibraries.AzureFunctions.Models.Users;
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
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;

namespace DZarsky.NotesFunction
{
    public sealed class AuthFunction
    {
        private readonly IConfiguration _configuration;
        private readonly UserService _userService;

        public AuthFunction(IConfiguration configuration, UserService userService)
        {
            _configuration = configuration;
            _userService = userService;
        }

        [FunctionName("SignUp")]
        [OpenApiOperation(operationId: "CreateUser", tags: new[] { Constants.AuthSectionName })]
        [OpenApiRequestBody(ApiConstants.JsonContentType, typeof(UserDto))]
        [OpenApiSecurity(ApiConstants.ApiKeyAuthSchemeID, SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = Constants.AuthApiKeyHeader)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: ApiConstants.JsonContentType, bodyType: typeof(User), Description = "Success")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: ApiConstants.JsonContentType, bodyType: typeof(ProblemDetails), Description = "Bad request")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: ApiConstants.JsonContentType, bodyType: typeof(ProblemDetails), Description = "Unauthorized")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Conflict, contentType: ApiConstants.JsonContentType, bodyType: typeof(ProblemDetails), Description = "Conflict")]
        public async Task<IActionResult> CreateUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = $"{Constants.AuthSectionName}/signup")] UserDto user, HttpRequest req)
        {
            var isApiKeyFilled = req.Headers.TryGetValue(Constants.AuthApiKeyHeader, out var apiKey);

            if (!isApiKeyFilled || apiKey.FirstOrDefault() != _configuration.GetValueFromContainer<string>("SignUpSecret"))
            {
                return new BadRequestObjectResult(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "API key is missing or incorrect",
                    Status = 401
                });
            }

            if (string.IsNullOrWhiteSpace(user.Login) || string.IsNullOrWhiteSpace(user.Password))
            {
                return new BadRequestObjectResult(new ProblemDetails
                {
                    Title = "Bad request",
                    Detail = "Login or password were empty",
                    Status = 400
                });
            }

            var result = await _userService.CreateUser(user);

            if (result.Status == ResultStatus.AlreadyExists)
            {
                return new ConflictObjectResult(new ProblemDetails
                {
                    Title = "Conflict",
                    Detail = "Record with given username already exists",
                    Status = 409
                });
            }
            else if (result.Status == ResultStatus.Failed)
            {
                return new BadRequestObjectResult(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Failed to create account",
                    Status = 400
                });
            }

            return new OkObjectResult(result.Result);
        }

        [FunctionName("GetUserInfo")]
        [OpenApiOperation(operationId: "GetUserInfo", tags: new[] { Constants.AuthSectionName })]
        [OpenApiRequestBody(ApiConstants.JsonContentType, typeof(UserDto))]
        [OpenApiSecurity(ApiConstants.ApiKeyAuthSchemeID, SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = Constants.AuthApiKeyHeader)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: ApiConstants.JsonContentType, bodyType: typeof(User), Description = "Success")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: ApiConstants.JsonContentType, bodyType: typeof(ProblemDetails), Description = "Bad request")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: ApiConstants.JsonContentType, bodyType: typeof(ProblemDetails), Description = "Unauthorized")]
        public async Task<IActionResult> GetInfo(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = $"{Constants.AuthSectionName}/info")] UserDto user, HttpRequest req)
        {
            var isApiKeyFilled = req.Headers.TryGetValue(Constants.AuthApiKeyHeader, out var apiKey);

            if (!isApiKeyFilled || apiKey.FirstOrDefault() != _configuration.GetValueFromContainer<string>("SignUpSecret"))
            {
                return new BadRequestObjectResult(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "API key is missing or incorrect",
                    Status = 401
                });
            }

            if (string.IsNullOrWhiteSpace(user.Login) || string.IsNullOrWhiteSpace(user.Password))
            {
                return new BadRequestObjectResult(new ProblemDetails
                {
                    Title = "Bad request",
                    Detail = "Login or password were empty",
                    Status = 400
                });
            }

            var result = await _userService.GetInfo(user);

            if (result.Status == ResultStatus.NotFound)
            {
                return new NotFoundObjectResult(new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = "Record does not exist",
                    Status = 404
                });
            }
            else if (result.Status == ResultStatus.Failed)
            {
                return new BadRequestObjectResult(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Failed to create account",
                    Status = 400
                });
            }

            return new OkObjectResult(result.Result);
        }
    }
}
