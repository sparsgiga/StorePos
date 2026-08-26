using Microsoft.Data.SqlClient;

namespace StorePos.IntegrationTests.Persistence;

public sealed class SqlServerTestDatabaseSafetyTests
{
    [Fact]
    public void DeleteGuard_AllowsOnlyExpectedLocalDbAndGuidDatabaseName()
    {
        var connectionString = BuildConnectionString(
            SqlServerTestDatabase.ExpectedDataSource,
            $"{SqlServerTestDatabase.DatabasePrefix}{Guid.NewGuid():N}");

        SqlServerTestDatabase.ValidateDeleteTarget(connectionString);
    }

    [Theory]
    [InlineData("(localdb)\\MSSQLLocalDB", "StorePos")]
    [InlineData("(localdb)\\MSSQLLocalDB", "StorePosTest")]
    [InlineData("(localdb)\\MSSQLLocalDB", "StorePosProduction")]
    [InlineData("(localdb)\\MSSQLLocalDB", "SomeOtherDatabase")]
    [InlineData("(localdb)\\MSSQLLocalDB", "StorePosIntegration_NOT_A_GUID")]
    [InlineData(".\\SQLEXPRESS", "StorePosIntegration_0fd4feeb278a4b9a85d406e24fb98e93")]
    public void DeleteGuard_RejectsEveryNonTestTarget(string dataSource, string databaseName)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            SqlServerTestDatabase.ValidateDeleteTarget(
                BuildConnectionString(dataSource, databaseName)));

        Assert.Equal("Refusing to delete a non-test database.", exception.Message);
    }

    private static string BuildConnectionString(string dataSource, string databaseName)
        => new SqlConnectionStringBuilder
        {
            DataSource = dataSource,
            InitialCatalog = databaseName,
            IntegratedSecurity = true,
            TrustServerCertificate = true
        }.ConnectionString;
}
