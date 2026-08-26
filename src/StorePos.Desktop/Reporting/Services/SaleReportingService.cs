using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using StorePos.Desktop.Reporting.Excel;
using StorePos.Desktop.Reporting.Models;
using StorePos.Desktop.Reporting.Printing;
using StorePos.Desktop.Reporting.ViewModels;
using StorePos.Desktop.Reporting.Views;

namespace StorePos.Desktop.Reporting.Services;

public sealed class SaleReportingService : ISaleReportingService
{
    private readonly SaleDocumentFactory _documentFactory = new();
    private readonly SaleExcelExporter _excelExporter = new();

    public void ShowOptions(FullSaleReportModel report)
    {
        var viewModel = new PrintOptionsDialogViewModel(
            report,
            PreviewFullSaleAsync,
            OpenLoadingListAsync,
            ExportExcelAsync);
        var dialog = new PrintOptionsDialog(viewModel)
        {
            Owner = GetActiveOwner()
        };
        dialog.ShowDialog();
    }

    private async Task PreviewFullSaleAsync(FullSaleReportModel report)
    {
        await Dispatcher.Yield(DispatcherPriority.Background);
        var viewModel = new PrintPreviewViewModel(
            $"გაყიდვა {report.SaleNumber}",
            size => _documentFactory.CreateFullSale(report, size));
        new PrintPreviewWindow(viewModel)
        {
            Owner = GetActiveOwner()
        }.ShowDialog();
    }

    private Task OpenLoadingListAsync(FullSaleReportModel report)
    {
        var viewModel = new LoadingListDialogViewModel(
            report,
            PreviewLoadingListAsync);
        new LoadingListDialog(viewModel)
        {
            Owner = GetActiveOwner()
        }.ShowDialog();
        return Task.CompletedTask;
    }

    private async Task PreviewLoadingListAsync(LoadingListReportModel report)
    {
        await Dispatcher.Yield(DispatcherPriority.Background);
        var viewModel = new PrintPreviewViewModel(
            $"დასატვირთი პროდუქცია — {report.SaleNumber}",
            size => _documentFactory.CreateLoadingList(report, size));
        new PrintPreviewWindow(viewModel)
        {
            Owner = GetActiveOwner()
        }.ShowDialog();
    }

    private async Task ExportExcelAsync(FullSaleReportModel report)
    {
        var dialog = new SaveFileDialog
        {
            Title = "გაყიდვის Excel ფაილის შენახვა",
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            DefaultExt = ".xlsx",
            AddExtension = true,
            FileName = ReportFileName.ForSale(report.SaleNumber),
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(GetActiveOwner()) != true)
        {
            return;
        }

        await _excelExporter.ExportAsync(dialog.FileName, report);
    }

    private static Window? GetActiveOwner()
        => Application.Current.Windows
               .OfType<Window>()
               .FirstOrDefault(window => window.IsActive)
           ?? Application.Current.MainWindow;
}
