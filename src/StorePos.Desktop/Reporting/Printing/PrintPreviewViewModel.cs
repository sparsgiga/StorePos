using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using StorePos.Desktop.Common;

namespace StorePos.Desktop.Reporting.Printing;

public sealed class PrintPreviewViewModel : ObservableObject
{
    private readonly Func<Size, FixedDocument> _documentFactory;
    private readonly RelayCommand _printCommand;
    private bool _isPrinting;
    private string? _errorMessage;

    public PrintPreviewViewModel(
        string title,
        Func<Size, FixedDocument> documentFactory)
    {
        Title = title;
        _documentFactory = documentFactory;
        Document = documentFactory(new Size(793.7, 1122.5));
        _printCommand = new RelayCommand(Print, () => !IsPrinting);
    }

    public string Title { get; }
    public FixedDocument Document { get; }

    public bool IsPrinting
    {
        get => _isPrinting;
        private set
        {
            if (SetProperty(ref _isPrinting, value))
            {
                _printCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public ICommand PrintCommand => _printCommand;

    private void Print()
    {
        try
        {
            IsPrinting = true;
            ErrorMessage = null;
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var printSize = new Size(
                Math.Max(dialog.PrintableAreaWidth, 300),
                Math.Max(dialog.PrintableAreaHeight, 400));
            var printDocument = _documentFactory(printSize);
            dialog.PrintDocument(printDocument.DocumentPaginator, Title);
        }
        catch (Exception exception)
        {
            ErrorMessage = $"ბეჭდვა ვერ შესრულდა: {exception.Message}";
        }
        finally
        {
            IsPrinting = false;
        }
    }
}
