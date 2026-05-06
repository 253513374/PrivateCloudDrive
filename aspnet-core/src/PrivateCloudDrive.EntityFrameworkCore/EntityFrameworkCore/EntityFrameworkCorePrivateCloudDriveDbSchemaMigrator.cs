using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrivateCloudDrive.Data;
using Volo.Abp.DependencyInjection;

namespace PrivateCloudDrive.EntityFrameworkCore;

public class EntityFrameworkCorePrivateCloudDriveDbSchemaMigrator
    : IPrivateCloudDriveDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCorePrivateCloudDriveDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolve the PrivateCloudDriveDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<PrivateCloudDriveDbContext>()
            .Database
            .MigrateAsync();
    }
}
