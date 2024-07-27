using Google.Apis.Auth.OAuth2;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DZarsky.NotesFunction.Infrastructure.Handlers;

public sealed class FirebaseClientAuthHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request, CancellationToken cancellationToken)
    {
        const string file = @"C\Temp\cucappka-0cb6316d92c2.json";
        
        if (!File.Exists(file))
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    "The Firebase service account file is required.")
            };
        }
        
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
        
        var serviceAccount = ServiceAccountCredential.FromServiceAccountData(stream);

        var token = await serviceAccount.GetAccessTokenForRequestAsync(cancellationToken: cancellationToken);
        
        request.Headers.Add("Authorization", $"Bearer {token}");
        

        return await base.SendAsync(request, cancellationToken);
    }
}
