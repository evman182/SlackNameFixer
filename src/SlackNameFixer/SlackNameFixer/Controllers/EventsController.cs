using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using SlackNameFixer.Persistence;

namespace SlackNameFixer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventsController : SlackControllerBase
    {
        private static readonly Regex EuroNameFormatRegex = new Regex(Constants.EuroNameFormatRegexString, RegexOptions.Compiled);

        private readonly ILogger<EventsController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SlackNameFixerContext _nameFixerContext;

        public EventsController(
            ILogger<EventsController> logger,
            IHttpClientFactory httpClientFactory,
            SlackNameFixerContext nameFixerContext)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _nameFixerContext = nameFixerContext;
        }

        [VerifySlackRequestSignature]
        [HttpPost]
        public async Task<IActionResult> ReceiveEvent()
        {

            string requestBody = await new StreamReader(Request.Body).ReadToEndAsync();
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
                var eventBody = parsedRequestBody.RootElement.GetProperty("event");
                var eventType = eventBody.GetProperty("type").GetString();
                _logger.LogInformation("Received event: {eventType}", eventType);

                if (eventType == "user_change")
                {
                    var userEntity = eventBody.GetProperty("user");
                    var realNameNormalized = userEntity.GetProperty("profile").GetProperty("real_name_normalized")
                        .GetString();

                    if (realNameNormalized != null && EuroNameFormatRegex.IsMatch(realNameNormalized))
                    {
                        var teamId = userEntity.GetProperty("team_id").GetString();
                        var userId = userEntity.GetProperty("id").GetString();

                        var user = 
                            _nameFixerContext.Users.SingleOrDefault(u => u.UserId == userId && u.TeamId == teamId);
                        if (user != null && !string.IsNullOrWhiteSpace(user.PreferredFullName))
                        {
                            var client = _httpClientFactory.CreateClient();
                            var message = new HttpRequestMessage(
                                HttpMethod.Post,
                                "https://slack.com/api/users.profile.set");
                            message.Content =
                                JsonContent.Create(new { name = "real_name", value = user.PreferredFullName });
                            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
                            await client.SendAsync(message);
                        }
                    }

                    return Ok();

                }
            }

            _logger.LogError("Doing something else?: {requestType}", requestType);
            return new ObjectResult("Not Supported Yet") { StatusCode = (int)HttpStatusCode.NotFound };
        }
    }
}