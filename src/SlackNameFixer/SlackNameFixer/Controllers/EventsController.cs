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
            if(requestBody.Contains("Evan", StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogInformation("requestBody: {requestBody}", requestBody);
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
                var eventBody = parsedRequestBody.RootElement.GetProperty("event");
                var eventType = eventBody.GetProperty("type").GetString();
                _logger.LogInformation("Received event: {eventType}", eventType);

                if (eventType == "user_change")
                {
                    var userEntity = eventBody.GetProperty("user");
                    var realName = userEntity.GetProperty("profile").GetProperty("real_name")
                        .GetString();

                    List<string> teamIds;
                    if (userEntity.TryGetProperty("enterprise_user", out var enterpriseUserEntity))
                    {
                        var teams = enterpriseUserEntity.GetProperty("teams");
                        var numTeams = teams.GetArrayLength();
                        teamIds = new List<string>(numTeams);

                        for (int x = 0; x < numTeams; x++)
                        {
                            teamIds.Add(teams[x].GetString());
                        }
                    }
                    else
                    {
                        var teamId = userEntity.GetProperty("team_id").GetString();
                        teamIds = new List<string> { teamId };
                    }
                    
                    var userId = userEntity.GetProperty("id").GetString();

                    _logger.LogInformation("teamIds: {teamIds}", string.Join(", ", teamIds));

                    var user =
                        _nameFixerContext.Users.SingleOrDefault(u => u.UserId == userId && teamIds.Contains(u.TeamId));

                        
                    _logger.LogInformation("user: {user}", user != null);

                    if (realName != null && 
                        user != null &&
                        !string.IsNullOrWhiteSpace(user.PreferredFullName) &&
                        realName != user.PreferredFullName)
                    {
                        _logger.LogInformation("TryUpdateUserFullName starting");
                       var updateResult = await _slackApi.TryUpdateUserFullName(user.AccessToken, user.PreferredFullName);
                       _logger.LogInformation("TryUpdateUserFullName result: {result}", updateResult);
                       if (updateResult == UpdateUserFullNameResult.InvalidToken)
                       {
                           _nameFixerContext.Remove(user); 
                           await _nameFixerContext.SaveChangesAsync();
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