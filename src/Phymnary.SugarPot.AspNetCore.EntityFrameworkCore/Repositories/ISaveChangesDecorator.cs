namespace Phymnary.SugarPot.AspNetCore.Repositories;

internal interface ISaveChangesDecorator
{
    Task SavingChangesAsync(Func<Task> mainDbSaveChangesAsync, CancellationToken cancellationToken);
}

internal class DefaultSaveChangesDecorator : ISaveChangesDecorator
{
    public Task SavingChangesAsync(
        Func<Task> mainDbSaveChangesAsync,
        CancellationToken cancellationToken
    )
    {
        return mainDbSaveChangesAsync();
    }
}
