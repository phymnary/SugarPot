using Phymnary.SugarPot.AspNetCore.Auditings;
using Phymnary.SugarPot.AspNetCore.Interceptors;

namespace Phymnary.SugarPot.AspNetCore.Repositories;

public class EfRepositoryAddons(
    EfDbStateManager dbStateManager,
    IEnumerable<IEfOnSavingEffect> savingInterceptors,
    IAbortedToken abortedProvider
)
{
    public EfDbStateManager DbStateManager => dbStateManager;

    public IEfOnSavingEffect[] SavingInterceptors => [.. savingInterceptors];

    public IAbortedToken AbortedProvider => abortedProvider;
}
