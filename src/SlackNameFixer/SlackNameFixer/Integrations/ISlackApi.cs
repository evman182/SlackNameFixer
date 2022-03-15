namespace SlackNameFixer.Integrations;

public interface ISlackApi
{
    Task<OAuthExchangeResult> ExchangeForToken(string code);

    Task<UpdateUserFullNameResult> TryUpdateUserFullName(string accessToken, string fullName);
}