namespace SlackNameFixer.Integrations;

public interface ISlackApi
{
    Task<OAuthExchangeResult> ExchangeForToken(string code);

    Task UpdateUserFullName(string accessToken, string fullName);
}