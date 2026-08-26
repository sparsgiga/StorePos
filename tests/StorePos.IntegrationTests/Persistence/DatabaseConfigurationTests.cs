using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace StorePos.IntegrationTests.Persistence;

public sealed class DatabaseConfigurationTests
{
    [Fact]
    public void RuntimeDatabaseConfigurations_AreIndependentAndNeverUseIntegrationPrefix()
    {
        var repositoryRoot = FindRepositoryRoot();
        var apiDirectory = Path.Combine(repositoryRoot, "src", "StorePos.Api");
        var common = ReadConnectionString(Path.Combine(apiDirectory, "appsettings.json"));
        var development = ReadConnectionString(
            Path.Combine(apiDirectory, "appsettings.Development.json"));
        var production = ReadConnectionString(
            Path.Combine(apiDirectory, "appsettings.Production.json"));

        Assert.Null(common);
        Assert.NotNull(development);
        Assert.NotNull(production);

        var developmentBuilder = new SqlConnectionStringBuilder(development);
        var productionBuilder = new SqlConnectionStringBuilder(production);
        Assert.Equal("(localdb)\\MSSQLLocalDB", developmentBuilder.DataSource);
        Assert.Equal("StorePosTest", developmentBuilder.InitialCatalog);
        Assert.Equal(".\\SQLEXPRESS", productionBuilder.DataSource);
        Assert.Equal("StorePos", productionBuilder.InitialCatalog);
        Assert.DoesNotContain(
            SqlServerTestDatabase.DatabasePrefix,
            development,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            SqlServerTestDatabase.DatabasePrefix,
            production,
            StringComparison.Ordinal);
    }

    private static string? ReadConnectionString(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.TryGetProperty("ConnectionStrings", out var connections) &&
               connections.TryGetProperty("StorePos", out var storePos)
            ? storePos.GetString()
            : null;
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "StorePos.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("StorePos repository root was not found.");
    }
}
