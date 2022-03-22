using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SlackNameFixer.Infrastructure;
using SlackNameFixer.Integrations;
using SlackNameFixer.Persistence;

namespace SlackNameFixer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private const string RegistrationLinkTemplate =
            "<https://slack.com/oauth/v2/authorize?client_id={0}&scope=commands&user_scope=users:read,users.profile:write|Click here to register>";

        private readonly ILogger<UsersController> _logger;
        private readonly ISlackApi _slackApi;
        private readonly SlackNameFixerContext _nameFixerContext;
        private readonly string _registrationLink;

        public UsersController(
            ILogger<UsersController> logger,
            ISlackApi slackApi,
            IOptions<SlackOptions> slackOptions,
            SlackNameFixerContext nameFixerContext)
        {
            _logger = logger;
            _slackApi = slackApi;
            _nameFixerContext = nameFixerContext;
            _registrationLink = string.Format(RegistrationLinkTemplate, slackOptions.Value.ClientId);
        }

        [VerifySlackRequestSignature]
        [HttpPost("get_preferred_name")]
        public ActionResult GetPreferredName([FromForm(Name = "user_id")]string userId, [FromForm(Name = "team_id")] string teamId)
        {
            var user = _nameFixerContext.Users.SingleOrDefault(u => u.UserId == userId && u.TeamId == teamId);
            if (user == null)
            {
                return Ok($"You are not registered with Slack Name Fixer or your registration has expired. {_registrationLink}");
            }

            if (string.IsNullOrWhiteSpace(user.PreferredFullName))
            {
                return Ok("Preferred Name is not set.");
            }

            return Ok($"Preferred Name is set to `{user.PreferredFullName}`");
        }



        [VerifySlackRequestSignature]
        [HttpPost("set_preferred_name")]
        public async Task<ActionResult> SetPreferredName([FromForm(Name = "user_id")] string userId, [FromForm(Name = "team_id")] string teamId, [FromForm] string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Ok("You cannot send an empty value for Preferred Name");
            }

            var user = _nameFixerContext.Users.SingleOrDefault(u => u.UserId == userId && u.TeamId == teamId);
            if (user == null)
            {
                return Ok($"You are not registered with Slack Name Fixer or your registration has expired. {_registrationLink}");
            }

            var trimmedName = text.Trim();
            var updateResult = await _slackApi.TryUpdateUserFullName(user.AccessToken, trimmedName);

            switch (updateResult)
            {
                case UpdateUserFullNameResult.InvalidToken:
                    _nameFixerContext.Remove(user);
                    await _nameFixerContext.SaveChangesAsync();
                    return Ok($"Your registration with Slack Name Fixer has expired. {_registrationLink}");

                case UpdateUserFullNameResult.Ok:
                    user.PreferredFullName = trimmedName;
                    await _nameFixerContext.SaveChangesAsync();
                    return Ok($"Preferred Name is now set to `{trimmedName}`");

                case UpdateUserFullNameResult.InvalidName:
                    return Ok($"Preferred Name cannot be set to {trimmedName}. Please ensure you are not using any special characters.");

                case UpdateUserFullNameResult.OtherError:
                default:
                    return Ok($"Error attempting to set Preferred Name to {trimmedName}");
            }
        }



        [VerifySlackRequestSignature]
        [HttpPost("unset_preferred_name")]
        public async Task<ActionResult> UnsetPreferredName([FromForm(Name = "user_id")] string userId, [FromForm(Name = "team_id")] string teamId)
        {
            var user = _nameFixerContext.Users.SingleOrDefault(u => u.UserId == userId && u.TeamId == teamId);
            if (user == null)
            {
                return Ok($"You are not registered with Slack Name Fixer or your registration has expired. {_registrationLink}");
            }
            
            user.PreferredFullName = null;
            await _nameFixerContext.SaveChangesAsync();

            return Ok("Preferred Name is now unset.");
        }
    }
}