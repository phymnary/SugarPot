using System.Net;

namespace Phymnary.SugarPot.AspNetCore.Exceptions;

public class TenantMissingInContextException(string message, Exception? innerException = null)
    : Exception(message, innerException),
        IDomainException
{
    public HttpStatusCode StatusCode => HttpStatusCode.Forbidden;

    public string? ErrorCode { get; private set; } =
        DomainErrorCodeRegistry.DefaultTenantMissingInContextErrorCode;

    public TenantMissingInContextException WithErrorCode(string code)
    {
        ErrorCode = code;
        return this;
    }
}
