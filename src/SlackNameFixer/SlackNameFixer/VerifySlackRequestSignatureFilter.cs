using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace SlackNameFixer;

public class VerifySlackRequestSignatureFilter : IAsyncActionFilter
{
    private readonly ILogger<VerifySlackRequestSignatureFilter> _logger;
    private readonly string _signingSecret;

    public VerifySlackRequestSignatureFilter(IOptions<SlackOptions> slackOptions, ILogger<VerifySlackRequestSignatureFilter> logger)
    {
        _logger = logger;
        _signingSecret = slackOptions.Value.SigningSecret;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await IsAuthenticRequest(context.HttpContext.Request))
        {
            context.Result = new UnauthorizedResult();
        }
        else
        {
            await next();
        }
    }

    private async Task<bool> IsAuthenticRequest(HttpRequest request)
    {
        try
        {
            request.Body.Position = 0;
            var requestBody = await new StreamReader(request.Body).ReadToEndAsync();
            var requestTimestamp = request.Headers["X-Slack-Request-Timestamp"];
            var requestSignature = request.Headers["X-Slack-Signature"];
            var currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            var requestTime = long.Parse(requestTimestamp);
            var timeSinceRequest = currentTime - requestTime;
            if (timeSinceRequest > 60 * 5)
            {
                _logger.LogError("Request too old");
                return false;
            }

            var baseString = $"v0:{requestTimestamp}:{requestBody}";

            var baseStringByteArray = Encoding.UTF8.GetBytes(baseString);
            using (var hasher = new HMACSHA256(Encoding.UTF8.GetBytes(_signingSecret)))
            {
                var hash = hasher.ComputeHash(baseStringByteArray);
                var hashHex = BitConverter.ToString(hash).Replace("-", string.Empty);
                var signature = $"v0={hashHex}";
                if (!string.Equals(signature, requestSignature, StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogError("Signature Verification failed");
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error on request signature verification");
            return false;
        }
        finally
        {
            request.Body.Position = 0;
        }
    }
}