using Microsoft.AspNetCore.Mvc;

namespace SlackNameFixer;

public class VerifySlackRequestSignatureAttribute : TypeFilterAttribute
{
    public VerifySlackRequestSignatureAttribute() : base(typeof(VerifySlackRequestSignatureFilter))
    {
    }
}