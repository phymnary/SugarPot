namespace Phymnary.SugarPot.AspNetCore.Interceptors;

public interface IEfOnSavingEffect
{
    ValueTask RunAsync(CancellationToken cancellationToken = default);
}
