using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SlackNameFixer.Infrastructure;
using SlackNameFixer.Integrations;
using SlackNameFixer.Persistence;

namespace SlackNameFixer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventsController : ControllerBase
    {

        private readonly ILogger<EventsController> _logger;
        private readonly ISlackApi _slackApi;
        private readonly SlackNameFixerContext _nameFixerContext;

        public EventsController(
            ILogger<EventsController> logger,
            ISlackApi slackApi,
            SlackNameFixerContext nameFixerContext)
        {
            _logger = logger;
            _slackApi = slackApi;
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
                    var realName = userEntity.GetProperty("profile").GetProperty("real_name")
                        .GetString();

                    var teamId = userEntity.GetProperty("team_id").GetString();
                    var userId = userEntity.GetProperty("id").GetString();

                    var user =
                        _nameFixerContext.Users.SingleOrDefault(u => u.UserId == userId && u.TeamId == teamId);

                    if (realName != null && 
                        user != null &&
                        !string.IsNullOrWhiteSpace(user.PreferredFullName) &&
                        realName != user.PreferredFullName)
                    {
                        await _slackApi.UpdateUserFullName(user.AccessToken, user.PreferredFullName);
                    }

                    return Ok();

                }
            }

            _logger.LogError("Doing something else?: {requestType}", requestType);
            return new ObjectResult("Not Supported Yet") { StatusCode = (int)HttpStatusCode.NotFound };
        }
    }
}