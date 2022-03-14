using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace SlackNameFixer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly ILogger<EventsController> _logger;
        private readonly string _signingSecret;

        public EventsController(ILogger<EventsController> logger, IOptions<SlackOptions> slackOptions)
        {
            _signingSecret = slackOptions.Value.SigningSecret;
            _logger = logger;

        }

        [HttpPost]
        public async Task<IActionResult> ReceiveEvent()
        {

            string requestBody = await new StreamReader(Request.Body).ReadToEndAsync();
            try
            {
                AuthenticateRequest(
                    requestBody,
                    Request.Headers["X-Slack-Request-Timestamp"],
                    Request.Headers["X-Slack-Signature"]);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to Authenticate: {ex.Message}");
                return new ObjectResult(ex.Message) { StatusCode = (int)HttpStatusCode.Unauthorized };
            }

            var parsedRequestBody = JsonDocument.Parse(requestBody);
            var requestType = parsedRequestBody.RootElement.GetProperty("type").GetString();
            if (requestType == "url_verification")
            {
                var challengeString = parsedRequestBody.RootElement.GetProperty("challenge").GetString();

                _logger.LogDebug("Responding to Challenge");
                return new OkObjectResult(challengeString);
            }

            if (requestType == "event_callback")
            {
                var eventType = parsedRequestBody.RootElement.GetProperty("event").GetProperty("type").GetString();
                if (eventType == "")
                {
                    _logger.LogInformation("Received event: {eventType}", eventType);
                    return Ok();
                    var client = new HttpClient();
                    var message = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/users.profile.set");
                    message.Headers.Authorization = new AuthenticationHeaderValue("");

                }
            }


            _logger.LogError("Doing something else?");
            return new ObjectResult("Not Supported Yet") { StatusCode = (int)HttpStatusCode.NotFound };
        }

        private void AuthenticateRequest(
            string requestBody,
            string requestTimestamp,
            string requestSignature)
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
            using (var hasher = new HMACSHA256(Encoding.UTF8.GetBytes(_signingSecret)))
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