using StorePos.Desktop.Reporting.Models;
using StorePos.Desktop.Reporting.ViewModels;

namespace StorePos.Desktop.Tests.Reporting;

public sealed class LoadingListDialogViewModelTests
{
    [Fact]
    public void ThreeItems_DefaultSelectedWithExactSoldQuantities()
    {
        var source = CreateReport(3);
        var viewModel = CreateViewModel(source);

        Assert.Equal(3, viewModel.Items.Count);
        Assert.All(viewModel.Items, item => Assert.True(item.IsSelected));
        Assert.Equal(source.Items.Select(item => item.Quantity),
            viewModel.Items.Select(item => item.LoadingQuantity));
    }

    [Theory]
    [InlineData("10", true, 10)]
    [InlineData("20", true, 20)]
    [InlineData("5,5", true, 5.5)]
    [InlineData("25", false, 20)]
    [InlineData("0", false, 20)]
    [InlineData("-1", false, 20)]
    public void LoadingQuantity_ValidatesAgainstSoldQuantity(
        string input,
        bool expectedValid,
        decimal expectedEffectiveQuantity)
    {
        var item = CreateViewModel(CreateReport(1)).Items[0];

        item.LoadingQuantityText = input;

        Assert.Equal(expectedValid, item.IsValid);
        Assert.Equal(expectedEffectiveQuantity, item.LoadingQuantity);
    }

    [Fact]
    public void BuildReport_IncludesOnlySelectedAndDoesNotMutateSourceQuantity()
    {
        var source = CreateReport(3);
        var originalQuantities = source.Items.Select(item => item.Quantity).ToArray();
        var viewModel = CreateViewModel(source);
        viewModel.Items[0].LoadingQuantityText = "10";
        viewModel.Items[1].IsSelected = false;

        var report = viewModel.BuildReport(new DateTime(2026, 8, 26, 12, 0, 0));

        Assert.Equal(2, report.Items.Count);
        Assert.Equal(10m, report.Items[0].LoadingQuantity);
        Assert.DoesNotContain(report.Items, item => item.SaleItemId == 2);
        Assert.Equal(originalQuantities, source.Items.Select(item => item.Quantity));
    }

    [Fact]
    public void LoadingReportModel_HasNoFinancialProperties()
    {
        var propertyNames = typeof(LoadingListReportModel)
            .GetProperties()
            .Concat(typeof(LoadingListReportItemModel).GetProperties())
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("UnitPrice", propertyNames);
        Assert.DoesNotContain("LineTotal", propertyNames);
        Assert.DoesNotContain("TotalAmount", propertyNames);
        Assert.DoesNotContain("PaidAmount", propertyNames);
        Assert.DoesNotContain("OutstandingAmount", propertyNames);
        Assert.DoesNotContain("PaymentType", propertyNames);
    }

    [Fact]
    public void ManualItemAndLongName_ArePreservedWithoutTruncation()
    {
        var longName = "მილი პოლიპროპილენის PN20 25მმ თეთრი ძალიან გრძელი სრული დასახელება";
        var source = CreateReport(1) with
        {
            Items =
            [
                CreateReport(1).Items[0] with
                {
                    ProductName = longName,
                    IsManual = true
                }
            ]
        };

        var report = CreateViewModel(source).BuildReport(DateTime.Now);

        Assert.Equal(longName, Assert.Single(report.Items).ProductName);
        Assert.True(Assert.Single(report.Items).IsManual);
    }

    [Fact]
    public void SelectAllAndClearAll_ArePurelyLocal()
    {
        var previewCalls = 0;
        var viewModel = new LoadingListDialogViewModel(
            CreateReport(3),
            _ =>
            {
                previewCalls++;
                return Task.CompletedTask;
            });

        viewModel.ClearAllCommand.Execute(null);
        Assert.All(viewModel.Items, item => Assert.False(item.IsSelected));
        viewModel.SelectAllCommand.Execute(null);
        Assert.All(viewModel.Items, item => Assert.True(item.IsSelected));
        Assert.Equal(0, previewCalls);
    }

    [Fact]
    public async Task Preview_IsExplicitAndRepeatedClickIsGuarded()
    {
        var calls = 0;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var viewModel = new LoadingListDialogViewModel(
            CreateReport(1),
            async _ =>
            {
                calls++;
                await completion.Task;
            });

        Assert.Equal(0, calls);
        viewModel.PreviewCommand.Execute(null);
        viewModel.PreviewCommand.Execute(null);
        await Task.Delay(20);

        Assert.Equal(1, calls);
        Assert.False(viewModel.PreviewCommand.CanExecute(null));
        completion.SetResult();
        await Task.Delay(20);
    }

    private static LoadingListDialogViewModel CreateViewModel(FullSaleReportModel report)
        => new(report, _ => Task.CompletedTask);

    private static FullSaleReportModel CreateReport(int itemCount)
        => new(
            1,
            "20260826-0001",
            1,
            "Customer",
            null,
            null,
            DateTime.Now,
            null,
            null,
            DateTime.Now,
            60m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            Enumerable.Range(1, itemCount)
                .Select(index => new FullSaleReportItemModel(
                    index,
                    $"P{index}",
                    null,
                    $"Product {index}",
                    "ცალი",
                    20m,
                    1m,
                    20m,
                    index == 1,
                    null))
                .ToArray());
}
