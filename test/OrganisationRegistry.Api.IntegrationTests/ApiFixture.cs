namespace OrganisationRegistry.Api.IntegrationTests
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using Infrastructure;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using OrganisationRegistry.Infrastructure;
    using SqlServer.Configuration;
    using SqlServer.Infrastructure;

    public class ApiFixture : IDisposable
    {
        private readonly IWebHost _webHost;
        public const string ApiEndpoint = "http://localhost:5000/v1/";
        public const string Jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhdF9oYXNoIjoiMklEMHdGR3l6WnJWaHRmbi00Ty1EQSIsImF1ZCI6WyJodHRwczovL2RpZW5zdHZlcmxlbmluZy10ZXN0LmJhc2lzcmVnaXN0ZXJzLnZsYWFuZGVyZW4iXSwiYXpwIjoiN2Q4MDExOTctNmQ0My00NzZhLTgzZWYtMzU4NjllZTUyZDg1IiwiZXhwIjoxODkzOTM2ODIzLCJmYW1pbHlfbmFtZSI6IkFwaSIsImdpdmVuX25hbWUiOiJUZXN0IiwiaWF0IjoxNTc4MzExNjMzLCJ2b19pZCI6IjEyMzk4Nzk4Ny0xMjMxMjMiLCJpc3MiOiJodHRwczovL2RpZW5zdHZlcmxlbmluZy10ZXN0LmJhc2lzcmVnaXN0ZXJzLnZsYWFuZGVyZW4iLCJ1cm46YmU6dmxhYW5kZXJlbjpkaWVuc3R2ZXJsZW5pbmc6YWNtaWQiOiJ2b19pZCIsInVybjpiZTp2bGFhbmRlcmVuOmFjbTpmYW1pbGllbmFhbSI6ImZhbWlseV9uYW1lIiwidXJuOmJlOnZsYWFuZGVyZW46YWNtOnZvb3JuYWFtIjoiZ2l2ZW5fbmFtZSIsInVybjpiZTp2bGFhbmRlcmVuOndlZ3dpanM6YWNtaWQiOiJ0ZXN0Iiwicm9sZSI6WyJhbGdlbWVlbkJlaGVlcmRlciJdLCJuYmYiOjE1NzgzOTY2MzN9.wWYDfwbcBxHMdaBIhoFH0UnXNl82lE_rsu-R49km1FM";
        public HttpClient HttpClient { get; } = new HttpClient
        {
            BaseAddress = new Uri(ApiEndpoint),
            DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Bearer", Jwt)}
        };

        public ApiFixture()
        {
            var maybeRootDirectory = Directory
                .GetParent(typeof(Startup).GetTypeInfo().Assembly.Location)?.Parent?.Parent?.Parent?.FullName;
            if (maybeRootDirectory is not { } rootDirectory)
                throw new NullReferenceException("Root directory cannot be null");

            Directory.SetCurrentDirectory(rootDirectory);

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{Environment.MachineName.ToLowerInvariant()}.json", optional: true);

            var connectionString =
                builder.Build()
                .GetSection(SqlServerConfiguration.Section)
                .Get<SqlServerConfiguration>()
                .MigrationsConnectionString;

            var dbContextOptions = new DbContextOptionsBuilder<OrganisationRegistryContext>()
                .UseSqlServer(
                    connectionString,
                    x => x.MigrationsHistoryTable("__EFMigrationsHistory", WellknownSchemas.BackofficeSchema))
                .Options;

            new OrganisationRegistryContext(dbContextOptions).Database.EnsureDeleted();

            IWebHostBuilder hostBuilder = new WebHostBuilder();
            var environment = hostBuilder.GetSetting("environment");

            if (environment == "Development")
            {
                var cert = new X509Certificate2("organisationregistry-api.pfx", "organisationregistry");

                hostBuilder = hostBuilder
                    .UseKestrel(server =>
                    {
                        server.AddServerHeader = false;
                        server.Listen(IPAddress.Any, 2443, listenOptions => listenOptions.UseHttps(cert));
                    })
                    .UseUrls("https://api.organisatie.dev-vlaanderen.local:2443");
            }
            else
            {
                hostBuilder = hostBuilder.UseKestrel(server => server.AddServerHeader = false);
            }

            _webHost = hostBuilder
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseConfiguration(builder.Build())
                .UseStartup<Startup>()
                .Build();

            _webHost.Start();

            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Import.Piavo.Program.Import(
                ApiEndpoint,
                Jwt);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                _webHost.Dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }
    }
}
