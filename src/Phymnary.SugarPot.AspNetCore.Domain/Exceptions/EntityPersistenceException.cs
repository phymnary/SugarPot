using System.Net;

namespace Phymnary.SugarPot.AspNetCore.Exceptions;

public class EntityPersistenceException(string message, Exception? innerException = null)
    : Exception(message, innerException),
        IDomainException
{
    public HttpStatusCode StatusCode => HttpStatusCode.Conflict;

    public string? ErrorCode { get; private set; } =
        DomainErrorCodeRegistry.DefaultEntityPersistenceErrorCode;

    public EntityPersistenceException WithErrorCode(string code)
    {
        ErrorCode = code;
        return this;
    }
}
