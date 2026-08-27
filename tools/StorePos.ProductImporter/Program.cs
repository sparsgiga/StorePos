using System.Diagnostics;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using StorePos.ProductImporter.Analysis;
using StorePos.ProductImporter.Database;
using StorePos.ProductImporter.Models;
using StorePos.ProductImporter.Parsing;

namespace StorePos.ProductImporter;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
        => RunAsync(args).GetAwaiter().GetResult();

    private static async Task<int> RunAsync(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("StorePos Product Importer");
        Console.WriteLine();

        try
        {
            var configuration = LoadConfiguration();
            var sourcePath = ResolveSourcePath(args);
            if (sourcePath is null)
            {
                Console.WriteLine("ფაილი არ არჩეულა.");
                return 1;
            }

            var connectionBuilder = new SqlConnectionStringBuilder(configuration.ConnectionString);
            Console.WriteLine($"Server:   {connectionBuilder.DataSource}");
            Console.WriteLine($"Database: {connectionBuilder.InitialCatalog}");
            Console.WriteLine($"Source:   {sourcePath}");
            Console.WriteLine("Mode:     Add new products only");
            Console.WriteLine();

            Console.WriteLine("Reading Excel...");
            var workbook = new ProductWorkbookParser().Parse(sourcePath);
            Console.WriteLine("Loading database metadata...");
            var database = new ProductImportDatabase(configuration.ConnectionString);
            var schema = await database.LoadSchemaAsync();
            var schemaResult = new ProductImportSchemaValidator().Validate(schema);
            if (!schemaResult.IsCompatible)
            {
                Console.WriteLine("Database schema is not compatible with this importer.");
                Console.WriteLine("Apply the latest StorePos database migrations first.");
                foreach (var error in schemaResult.Errors)
                {
                    Console.WriteLine($"  - {error}");
                }
                return 2;
            }

            Console.WriteLine("Resolving units and comparing Products...");
            var referenceData = await database.LoadReferenceDataAsync();
            var dryRun = new DryRunAnalyzer().Analyze(workbook, referenceData);
            PrintDryRun(dryRun);
            if (dryRun.HasBlockingIssues)
            {
                Console.WriteLine("Blocking conflicts/errors exist. Import is disabled.");
                return 3;
            }

            if (dryRun.NewProducts.Count == 0)
            {
                Console.WriteLine("No new Products to import.");
                return 0;
            }

            Console.WriteLine();
            Console.Write(
                $"Import {dryRun.NewProducts.Count:N0} new products into database " +
                $"{connectionBuilder.InitialCatalog}? [Y/N] ");
            var confirmation = Console.ReadLine()?.Trim();
            if (!string.Equals(confirmation, "Y", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Import cancelled. No Product data was changed.");
                return 0;
            }

            Console.WriteLine("Importing...");
            var result = await database.ImportAsync(dryRun.NewProducts);
            Console.WriteLine("Import completed successfully.");
            Console.WriteLine();
            Console.WriteLine($"Source rows:          {dryRun.SourceRowCount,8:N0}");
            Console.WriteLine($"Inserted:             {result.InsertedCount,8:N0}");
            Console.WriteLine($"Existing skipped:     {dryRun.ExistingCount,8:N0}");
            Console.WriteLine($"Duplicate identical:  {dryRun.DuplicateIdenticalCount,8:N0}");
            Console.WriteLine($"Price = 0:            {dryRun.ZeroPriceCount,8:N0}");
            Console.WriteLine($"Missing Barcode:      {dryRun.MissingBarcodeCount,8:N0}");
            Console.WriteLine($"Warnings/info:        {dryRun.WarningCount,8:N0}");
            Console.WriteLine($"Duration:              {result.Duration.TotalSeconds,7:N1}s");
            return 0;
        }
        catch (ProductImportTransactionException exception)
        {
            Console.Error.WriteLine("Import failed.");
            if (exception.RollbackSucceeded)
            {
                Console.Error.WriteLine("Transaction rolled back.");
                Console.Error.WriteLine("No Products from this import were saved.");
            }
            else
            {
                Console.Error.WriteLine("Rollback could not be confirmed. Inspect the target database.");
            }
            Console.Error.WriteLine(SafeMessage(exception.InnerException ?? exception));
            return 4;
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            Console.Error.WriteLine($"Operation failed: {SafeMessage(exception)}");
            return 5;
        }
    }

    private static ImporterConfiguration LoadConfiguration()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Importer appsettings.json was not found.", path);
        }

        var configuration = JsonSerializer.Deserialize<ImporterConfiguration>(File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (string.IsNullOrWhiteSpace(configuration?.ConnectionString))
        {
            throw new InvalidDataException("Importer ConnectionString is missing.");
        }

        return configuration;
    }

    private static string? ResolveSourcePath(string[] args)
    {
        if (args.Length > 0)
        {
            var path = Path.GetFullPath(args[0]);
            if (!File.Exists(path) || !string.Equals(Path.GetExtension(path), ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                throw new FileNotFoundException("The supplied .xlsx file does not exist.", path);
            }
            return path;
        }

        using var dialog = new System.Windows.Forms.OpenFileDialog
        {
            Title = "პროდუქტების Excel ფაილის არჩევა",
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            CheckFileExists = true,
            Multiselect = false
        };
        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
            ? dialog.FileName
            : null;
    }

    private static void PrintDryRun(DryRunResult result)
    {
        Console.WriteLine("Dry Run complete. No Product data was changed.");
        Console.WriteLine();
        Console.WriteLine($"ფაილის ჩანაწერები:     {result.SourceRowCount,8:N0}");
        Console.WriteLine($"ახალი:                 {result.NewProducts.Count,8:N0}");
        Console.WriteLine($"უკვე არსებობს:         {result.ExistingCount,8:N0}");
        Console.WriteLine($"იდენტური დუბლიკატი:    {result.DuplicateIdenticalCount,8:N0}");
        Console.WriteLine($"ფასი 0:                {result.ZeroPriceCount,8:N0}");
        Console.WriteLine($"Barcode-ის გარეშე:     {result.MissingBarcodeCount,8:N0}");
        Console.WriteLine($"გაფრთხილება/info:      {result.WarningCount,8:N0}");
        Console.WriteLine($"კონფლიქტი:             {result.ConflictCount,8:N0}");
        Console.WriteLine($"შეცდომა:               {result.ErrorCount,8:N0}");

        foreach (var issue in result.Issues.Where(issue => issue.IsBlocking))
        {
            Console.WriteLine();
            Console.WriteLine($"Row {issue.ExcelRow?.ToString() ?? "—"}: {issue.Severity}");
            Console.WriteLine($"Code: {issue.Code ?? "—"}");
            Console.WriteLine($"Product: {issue.ProductName ?? "—"}");
            Console.WriteLine($"{issue.Field}: {issue.Value ?? "—"}");
            Console.WriteLine(issue.Message);
        }
    }

    private static string SafeMessage(Exception exception)
        => exception is SqlException sqlException
            ? $"SQL error {sqlException.Number}: {sqlException.Message}"
            : exception.Message;

    private sealed record ImporterConfiguration(string ConnectionString);
}
