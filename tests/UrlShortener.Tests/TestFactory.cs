using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Api.Data;

namespace UrlShortener.Tests;

public class TestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName =
        $"testdb_{Guid.NewGuid():N}";
    private SqliteConnection? _keepAlive;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connStr =
            $"Data Source={_dbName};Mode=Memory;Cache=Shared";

        _keepAlive = new SqliteConnection(connStr);
        _keepAlive.Open();

        builder.ConfigureServices(services =>
        {
            var desc = services.SingleOrDefault(
                d => d.ServiceType ==
                     typeof(DbContextOptions<AppDbContext>));
            if (desc != null) services.Remove(desc);
            services.AddDbContext<AppDbContext>(opts =>
                opts.UseSqlite(connStr));

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Testing");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _keepAlive?.Close();
            _keepAlive?.Dispose();
        }
        base.Dispose(disposing);
    }
}