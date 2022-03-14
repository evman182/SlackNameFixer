using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SlackNameFixer
{
    [UsedImplicitly]
    public static class SlackNameFixer
    {
        private const string SigningSecret = "";

        [UsedImplicitly]
        [FunctionName("Endpoint")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            try
            {
                AuthenticateRequest(
                    requestBody, 
                    req.Headers["X-Slack-Request-Timestamp"],
                    req.Headers["X-Slack-Signature"],
                    log);
            }
            catch (Exception ex)
            {
                log.LogError($"Failed to Authenticate: {ex.Message}");
                return new ObjectResult(ex.Message) { StatusCode = (int)HttpStatusCode.Unauthorized };
            }

            var parsedRequestBody = JsonDocument.Parse(requestBody);
            var requestType = parsedRequestBody.RootElement.GetProperty("type").GetString();
            if (requestType == "url_verification")
            {
                var challengeString = parsedRequestBody.RootElement.GetProperty("challenge").GetString();

                log.LogDebug("Responding to Challenge");
                return new OkObjectResult(challengeString);
            }

            if (requestType == "event_callback")
            {
                var eventType = parsedRequestBody.RootElement.GetProperty("event").GetProperty("type").GetString();
                if (eventType == "")
                {
                    var client = new HttpClient();
                    var message = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/users.profile.set");
                    message.Headers.Authorization = new AuthenticationHeaderValue("");

                }
            }


            log.LogError("Doing something else?");
            return new ObjectResult("Not Supported Yet") { StatusCode = (int)HttpStatusCode.NotFound };
        }

        private static void AuthenticateRequest(
            string requestBody, 
            string requestTimestamp, 
            string requestSignature,
            ILogger logger)
        {
            var currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            var requestTime = long.Parse(requestTimestamp);
            var timeSinceRequest = currentTime - requestTime;
            if (timeSinceRequest > 60 * 5)
            {
                throw new Exception("Request too old");
            }

            var baseString = $"v0:{requestTimestamp}:{requestBody}";

            var baseStringByteArray = Encoding.UTF8.GetBytes(baseString);
            using (var hasher = new HMACSHA256(Encoding.UTF8.GetBytes(SigningSecret)))
            {
                var hash = hasher.ComputeHash(baseStringByteArray);
                var hashHex = BitConverter.ToString(hash).Replace("-", string.Empty);
                var signature = $"v0={hashHex}";
                if (!string.Equals(signature, requestSignature, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new Exception("Signature Verification failed");
                }
            }
        }
    }
}
