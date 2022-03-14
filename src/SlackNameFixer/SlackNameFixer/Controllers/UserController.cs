using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using SlackNameFixer.Infrastructure;
using SlackNameFixer.Persistence;

namespace SlackNameFixer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private static readonly Regex EuroNameFormatRegex = new Regex(Constants.EuroNameFormatRegexString, RegexOptions.Compiled);

        private const string RegistrationLink =
            "<https://slack.com/oauth/v2/authorize?client_id=34873463795.2439460978368&scope=commands&user_scope=users:read,users.profile:write|Click here to register>";

        private readonly ILogger<UsersController> _logger;
        private readonly SlackNameFixerContext _nameFixerContext;
        private readonly IHttpClientFactory _httpClientFactory;

        public UsersController(
            ILogger<UsersController> logger,
            SlackNameFixerContext nameFixerContext,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _nameFixerContext = nameFixerContext;
            _httpClientFactory = httpClientFactory;
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
            if (EuroNameFormatRegex.IsMatch(trimmedName))
            {
                return Ok("You cannot use a Preferred Name in the format of `First LAST`");
            }

            user.PreferredFullName = trimmedName;
            await _nameFixerContext.SaveChangesAsync();

            return Ok($"Preferred Name is now set to `{trimmedName}`");
        }
    }
}