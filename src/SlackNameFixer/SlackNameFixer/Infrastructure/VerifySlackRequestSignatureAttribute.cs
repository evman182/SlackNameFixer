using Microsoft.AspNetCore.Mvc;

namespace SlackNameFixer.Infrastructure;

public class VerifySlackRequestSignatureAttribute : TypeFilterAttribute
{
    public VerifySlackRequestSignatureAttribute() : base(typeof(VerifySlackRequestSignatureFilter))
    {
    }
}