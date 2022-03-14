namespace SlackNameFixer.Integrations;

public interface ISlackApi
{
    Task<OAuthExchangeResult> ExchangeForToken(string code);

    Task<bool> TryUpdateUserFullName(string accessToken, string fullName);
}