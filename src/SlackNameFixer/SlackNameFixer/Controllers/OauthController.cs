using Microsoft.AspNetCore.Mvc;
using SlackNameFixer.Integrations;
using SlackNameFixer.Persistence;

namespace SlackNameFixer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OauthController : ControllerBase
    {

        private readonly ILogger<OauthController> _logger;
        private readonly SlackNameFixerContext _nameFixerContext;
        private readonly ISlackApi _slackApi;

        public OauthController(
            ILogger<OauthController> logger,
            SlackNameFixerContext nameFixerContext,
            ISlackApi slackApi)
        {
            _logger = logger;
            _nameFixerContext = nameFixerContext;
            _slackApi = slackApi;
        }

        [HttpGet("redirect")]
        public async Task<IActionResult> ProcessRedirect(string code)
        {
            var oauthExchangeInfo = await _slackApi.ExchangeForToken(code);

            if (oauthExchangeInfo == null)
            {
                return Unauthorized();
            }

            var existingUser = _nameFixerContext.Users
                .SingleOrDefault(u => u.TeamId == oauthExchangeInfo.TeamId && u.UserId == oauthExchangeInfo.UserId);
            if (existingUser == null)
            {
                _nameFixerContext.Users.Add(new User
                {
                    TeamId = oauthExchangeInfo.TeamId,
                    UserId = oauthExchangeInfo.UserId,
                    AccessToken = oauthExchangeInfo.AccessToken,
                });
            }
            else
            {
                existingUser.AccessToken = oauthExchangeInfo.AccessToken;
            }

            await _nameFixerContext.SaveChangesAsync();

            return Ok("Successfully Authorized App. Return to slack and use the \"/set_preferred_name\" command to set your Preferred Name");
        }
    }
}