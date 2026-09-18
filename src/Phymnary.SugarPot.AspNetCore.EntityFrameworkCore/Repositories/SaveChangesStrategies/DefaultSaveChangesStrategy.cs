namespace Phymnary.SugarPot.AspNetCore.Repositories.SaveChangesStrategies;

internal class DefaultSaveChangesStrategy : ISaveChangesStrategy
{
    public Task SavingChangesAsync(
        Func<CancellationToken, Task> mainDbSaveChangesAsync,
        CancellationToken cancellationToken
    )
    {
        return mainDbSaveChangesAsync(cancellationToken);
    }
}
