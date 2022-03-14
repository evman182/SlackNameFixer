using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SlackNameFixer.Infrastructure;

namespace SlackNameFixer.Integrations;

public class SlackApi : ISlackApi
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SlackApi> _logger;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public SlackApi(IHttpClientFactory httpClientFactory, IOptions<SlackOptions> options, ILogger<SlackApi> logger)
    {
        _clientId = options.Value.ClientId;
        _clientSecret = options.Value.ClientSecret;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<OAuthExchangeResult> ExchangeForToken(string code)
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
            return null;
        }

        var responseBody = await result.Content.ReadAsStringAsync();
        var parsedBody = JsonDocument.Parse(responseBody);
        var isOk = parsedBody.RootElement.GetProperty("ok").GetBoolean();
        if (!isOk)
        {
            _logger.LogError("OAuth exchange response not OK: {responseBody}", responseBody);
            return null;
        }

        var authedUser = parsedBody.RootElement.GetProperty("authed_user");
        var tokenType = authedUser.GetProperty("token_type").GetString();

        if (tokenType != "user")
        {
            return null;
        }

        var teamId = parsedBody.RootElement.GetProperty("team").GetProperty("id").GetString();
        var userId = authedUser.GetProperty("id").GetString();
        var accessToken = authedUser.GetProperty("access_token").GetString();
        return new OAuthExchangeResult
        {
            TeamId = teamId,
            UserId = userId,
            AccessToken = accessToken,
        };
    }

    public async Task<bool> TryUpdateUserFullName(string accessToken, string fullName)
    {
        var client = _httpClientFactory.CreateClient();
        var message = new HttpRequestMessage(
            HttpMethod.Post,
            "https://slack.com/api/users.profile.set");
        message.Content =
            JsonContent.Create(new { name = "real_name", value = fullName });
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var result = await client.SendAsync(message);
        var responseBody = await result.Content.ReadAsStringAsync();
        var parsedResponse = JsonDocument.Parse(responseBody);
        var isOk = parsedResponse.RootElement.GetProperty("ok").GetBoolean();
        return isOk;
    }
}