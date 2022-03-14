using Microsoft.AspNetCore.Mvc;
using SlackNameFixer.Infrastructure;
using SlackNameFixer.Integrations;
using SlackNameFixer.Persistence;

namespace SlackNameFixer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private const string RegistrationLink =
            "<https://slack.com/oauth/v2/authorize?client_id=34873463795.2439460978368&scope=commands&user_scope=users:read,users.profile:write|Click here to register>";

        private readonly ILogger<UsersController> _logger;
        private readonly ISlackApi _slackApi;
        private readonly SlackNameFixerContext _nameFixerContext;

        public UsersController(
            ILogger<UsersController> logger,
            ISlackApi slackApi,
            SlackNameFixerContext nameFixerContext)
        {
            _logger = logger;
            _slackApi = slackApi;
            _nameFixerContext = nameFixerContext;
        }

        [VerifySlackRequestSignature]
        [HttpPost("get_preferred_name")]
        public ActionResult GetPreferredName([FromForm(Name = "user_id")]string userId, [FromForm(Name = "team_id")] string teamId)
        {
            var user = _nameFixerContext.Users.SingleOrDefault(u => u.UserId == userId && u.TeamId == teamId);
            if (user == null)
            {
                return Ok($"You are not registered with Slack Name Fixer. {RegistrationLink}");
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
            var user = _nameFixerContext.Users.SingleOrDefault(u => u.UserId == userId && u.TeamId == teamId);
            if (user == null)
            {
                return Ok($"You are not registered with Slack Name Fixer. {RegistrationLink}");
            }

            var trimmedName = text.Trim();
            var canUpdate = await _slackApi.TryUpdateUserFullName(user.AccessToken, trimmedName);

            if (!canUpdate)
            {
                return Ok($"Preferred Name cannot be set to {trimmedName}");
            }

            user.PreferredFullName = trimmedName;
            await _nameFixerContext.SaveChangesAsync();

            return Ok($"Preferred Name is now set to `{trimmedName}`");
        }



        [VerifySlackRequestSignature]
        [HttpPost("unset_preferred_name")]
        public async Task<ActionResult> UnsetPreferredName([FromForm(Name = "user_id")] string userId, [FromForm(Name = "team_id")] string teamId)
        {
            var user = _nameFixerContext.Users.SingleOrDefault(u => u.UserId == userId && u.TeamId == teamId);
            if (user == null)
            {
                return Ok($"You are not registered with Slack Name Fixer. {RegistrationLink}");
            }
            
            user.PreferredFullName = null;
            await _nameFixerContext.SaveChangesAsync();

            return Ok("Preferred Name is now unset.");
        }
    }
}