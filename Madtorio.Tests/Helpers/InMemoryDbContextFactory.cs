using Madtorio.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Madtorio.Tests.Helpers;

/// <summary>
/// Factory for creating in-memory database contexts for testing.
/// Each context uses a unique database name to ensure test isolation.
/// </summary>
public static class InMemoryDbContextFactory
{
    /// <summary>
    /// Creates a new ApplicationDbContext with an in-memory database.
    /// Uses a unique GUID for the database name to ensure isolation between tests.
    /// </summary>
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);

        // Ensure the database is created
        context.Database.EnsureCreated();

        return context;
    }

    /// <summary>
    /// Creates a new ApplicationDbContext with a specified database name.
    /// Useful when you need multiple contexts sharing the same database.
    /// </summary>
    public static ApplicationDbContext CreateWithName(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }
}
