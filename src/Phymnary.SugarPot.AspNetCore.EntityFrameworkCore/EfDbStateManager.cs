using Microsoft.EntityFrameworkCore;
using Phymnary.SugarPot.AspNetCore.Exceptions;

namespace Phymnary.SugarPot.AspNetCore;

public class EfDbStateManager
{
    internal bool IsExecutionStrategyInTransaction { get; private set; }

    internal async Task ExecuteStrategyInTransaction(
        Func<CancellationToken, Task> execution,
        CancellationToken cancellationToken
    )
    {
        IsExecutionStrategyInTransaction = true;
        try
        {
            await execution(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new EntityPersistenceException(
                "Db throw exception when was executing strategy",
                ex
            );
        }
        finally
        {
            IsExecutionStrategyInTransaction = false;
        }
    }
}
