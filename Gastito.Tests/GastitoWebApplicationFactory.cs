using System.Net.Http.Headers;
using System.Net.Http.Json;
using Gastito.DataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gastito.Tests;

public class GastitoWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "GastitoTestDb_" + Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing AppDbContext descriptor
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Register InMemory database
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

            // Ensure database and seed data are created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            usuario = "admin",
            contrasena = "admin"
        });

        loginResponse.EnsureSuccessStatusCode();

        var result = await loginResponse.Content.ReadFromJsonAsync<LoginTestResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result!.Token);

        return client;
    }

    public class LoginTestResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiraEnUtc { get; set; }
    }
}
