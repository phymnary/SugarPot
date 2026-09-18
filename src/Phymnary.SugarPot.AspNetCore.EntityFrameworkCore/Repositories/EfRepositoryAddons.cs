using Phymnary.SugarPot.AspNetCore.Interceptors;
using Phymnary.SugarPot.AspNetCore.Repositories.SaveChangesStrategies;

namespace Phymnary.SugarPot.AspNetCore.Repositories;

public class EfRepositoryAddons(
    EfDbStateManager dbStateManager,
    IEnumerable<IEfOnSavingEffect> savingInterceptors,
    ISaveChangesStrategy saveChangesStrategy,
    IAbortedToken abortedProvider
)
{
    public EfDbStateManager DbStateManager => dbStateManager;

    public IEfOnSavingEffect[] OnSavingInterceptors => [.. savingInterceptors];

    public ISaveChangesStrategy SaveChangesStrategy => saveChangesStrategy;

    public IAbortedToken AbortedProvider => abortedProvider;
}
