using Microsoft.EntityFrameworkCore;
using MyDockerProject.Data;

namespace MyDockerProject.Services;

public sealed class BookingManagementSchemaInitializer : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingManagementSchemaInitializer> _logger;

    public BookingManagementSchemaInitializer(IServiceScopeFactory scopeFactory, ILogger<BookingManagementSchemaInitializer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        try
        {
            await database.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'[dbo].[Bookings]', N'U') IS NOT NULL
                    AND COL_LENGTH(N'dbo.Bookings', N'ManagementTokenHash') IS NULL
                    ALTER TABLE [dbo].[Bookings] ADD [ManagementTokenHash] nvarchar(64) NULL;
                """, cancellationToken);

            await database.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'[dbo].[Bookings]', N'U') IS NOT NULL
                    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Bookings]') AND name = N'IX_Bookings_ManagementTokenHash')
                    EXEC(N'CREATE UNIQUE INDEX [IX_Bookings_ManagementTokenHash] ON [dbo].[Bookings] ([ManagementTokenHash]) WHERE [ManagementTokenHash] IS NOT NULL');
                """, cancellationToken);
            _logger.LogInformation("Booking management credential schema is ready.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not initialize booking management credential schema.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
