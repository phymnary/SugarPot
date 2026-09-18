namespace Phymnary.SugarPot.AspNetCore.Repositories.SaveChangesStrategies;

public interface ISaveChangesStrategy
{
    Task SavingChangesAsync(
        Func<CancellationToken, Task> mainDbSaveChangesAsync,
        CancellationToken cancellationToken
    );
}
