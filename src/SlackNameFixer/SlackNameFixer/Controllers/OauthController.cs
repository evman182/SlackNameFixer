using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SlackNameFixer.Infrastructure;
using SlackNameFixer.Persistence;

namespace SlackNameFixer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OauthController : ControllerBase
    {

        private readonly ILogger<OauthController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SlackNameFixerContext _nameFixerContext;
        private readonly string _clientId;
        private readonly string _clientSecret;

        public OauthController(
            ILogger<OauthController> logger,
            IHttpClientFactory httpClientFactory,
            SlackNameFixerContext nameFixerContext,
            IOptions<SlackOptions> options)
        {
            _clientId = options.Value.ClientId;
            _clientSecret = options.Value.ClientSecret;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _nameFixerContext = nameFixerContext;
        }

        [HttpGet("redirect")]
        public async Task<IActionResult> ProcessRedirect(string code)
        {
            var client = _httpClientFactory.CreateClient();
            var message = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/oauth.v2.access");

            var authString = $"{_clientId}:{_clientSecret}";
            message.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(authString)));

            var content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>> { new("code", code) });
            message.Content = content;
            var result = await client.SendAsync(message);
            if (!result.IsSuccessStatusCode)
            {
                _logger.LogError("Unsuccessful oauth exchange");
                return Unauthorized();
            }

            var responseBody = await result.Content.ReadAsStringAsync();
            var parsedBody = JsonDocument.Parse(responseBody);
            var isOk = parsedBody.RootElement.GetProperty("ok").GetBoolean();
            if (!isOk)
            {
                _logger.LogError("OAuth exchange response not OK: {responseBody}", responseBody);
                return Unauthorized();
            }

            var authedUser = parsedBody.RootElement.GetProperty("authed_user");
            var tokenType= authedUser.GetProperty("token_type").GetString();

            if (tokenType != "user")
            {
                return Unauthorized();
            }

            var teamId = parsedBody.RootElement.GetProperty("team").GetProperty("id").GetString();
            var userId = authedUser.GetProperty("id").GetString();
            var accessToken = authedUser.GetProperty("access_token").GetString();

            var existingUser = _nameFixerContext.Users.SingleOrDefault(u => u.TeamId == teamId && u.UserId == userId);
            if (existingUser == null)
            {
                _nameFixerContext.Users.Add(new User
                {
                    TeamId = teamId,
                    UserId = userId,
                    AccessToken = accessToken,
                });
            }
            else
            {
                existingUser.AccessToken = accessToken;
            }

            await _nameFixerContext.SaveChangesAsync();

            return Ok("Successfully Authorized App");
        }
    }
}