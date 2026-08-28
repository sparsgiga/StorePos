using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using StorePos.Desktop.Reporting.Models;

namespace StorePos.Desktop.Reporting.Printing;

public sealed class SaleDocumentFactory
{
    private const double A4Width = 793.7;
    private const double A4Height = 1122.5;
    private const double Margin = 42;
    private static readonly FontFamily GeorgianFont = new("Sylfaen");

    public FixedDocument CreateFullSale(
        FullSaleReportModel report,
        Size? requestedPageSize = null)
    {
        var composer = new PageComposer(NormalizePageSize(requestedPageSize));
        composer.StartPage(() => CreateFullHeader(report, composer.ContentWidth),
            () => CreateFullTableHeader(composer.ContentWidth));

        foreach (var item in report.Items)
        {
            composer.Add(
                () => CreateFullItemRow(item, composer.ContentWidth),
                () => CreateFullHeader(report, composer.ContentWidth),
                () => CreateFullTableHeader(composer.ContentWidth));
        }

        composer.Add(
            () => CreateFinancialSummary(report, composer.ContentWidth),
            () => CreateFullHeader(report, composer.ContentWidth),
            () => CreateFullTableHeader(composer.ContentWidth));

        return composer.Finish();
    }

    public FixedDocument CreateLoadingList(
        LoadingListReportModel report,
        Size? requestedPageSize = null)
    {
        var composer = new PageComposer(NormalizePageSize(requestedPageSize));
        composer.StartPage(() => CreateLoadingHeader(report, composer.ContentWidth));

        if (!string.IsNullOrWhiteSpace(report.PrintComment))
        {
            composer.Add(
                () => CreatePrintComment(report.PrintComment!, composer.ContentWidth),
                () => CreateLoadingHeader(report, composer.ContentWidth));
        }

        foreach (var item in report.Items)
        {
            composer.Add(
                () => CreateLoadingItem(item, composer.ContentWidth),
                () => CreateLoadingHeader(report, composer.ContentWidth));
        }

        return composer.Finish();
    }

    private static Size NormalizePageSize(Size? requested)
    {
        if (requested is { Width: > 300, Height: > 400 } size)
        {
            return size;
        }

        return new Size(A4Width, A4Height);
    }

    private static FrameworkElement CreateFullHeader(
        FullSaleReportModel report,
        double width)
    {
        var panel = new StackPanel { Width = width, Margin = new Thickness(0, 0, 0, 10) };
        panel.Children.Add(Text("გაყიდვის სრული ანგარიში", 20, FontWeights.Bold,
            TextAlignment.Center));
        panel.Children.Add(Text(report.Status == 1
            ? "დაუსრულებელი გაყიდვა"
            : ReportFormatting.Status(report.Status),
            13,
            FontWeights.Bold,
            TextAlignment.Center,
            report.Status == 1 ? Brushes.DarkRed : Brushes.Black));

        var metadata = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        metadata.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        metadata.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var left = new StackPanel();
        left.Children.Add(LabelValue("გაყიდვა", report.SaleNumber));
        left.Children.Add(LabelValue("მყიდველი", report.CustomerName ?? "—"));
        left.Children.Add(LabelValue("ს/ნ", report.CustomerIdentificationNumber ?? "—"));
        var right = new StackPanel();
        right.Children.Add(LabelValue("შექმნილია", report.DateCreated.ToString("dd.MM.yyyy HH:mm")));
        right.Children.Add(LabelValue("დასრულებულია", report.DateCompleted?.ToString("dd.MM.yyyy HH:mm") ?? "—"));
        right.Children.Add(LabelValue("დაბეჭდილია", report.PrintedAt.ToString("dd.MM.yyyy HH:mm")));
        metadata.Children.Add(left);
        Grid.SetColumn(right, 1);
        metadata.Children.Add(right);
        panel.Children.Add(metadata);

        if (!string.IsNullOrWhiteSpace(report.Comment))
        {
            panel.Children.Add(Text($"კომენტარი: {report.Comment}", 10, FontWeights.Normal,
                margin: new Thickness(0, 7, 0, 0)));
        }

        return panel;
    }

    private static FrameworkElement CreateFullTableHeader(double width)
    {
        var grid = CreateFullGrid(width);
        grid.Background = new SolidColorBrush(Color.FromRgb(232, 236, 241));
        AddCell(grid, 0, "კოდი", true);
        AddCell(grid, 1, "შტრიხკოდი", true);
        AddCell(grid, 2, "პროდუქტი", true);
        AddCell(grid, 3, "ერთ.", true);
        AddCell(grid, 4, "რაოდენობა", true, TextAlignment.Right);
        AddCell(grid, 5, "ფასი", true, TextAlignment.Right);
        AddCell(grid, 6, "ჯამი", true, TextAlignment.Right);
        return WrapRow(grid, new Thickness(0), Brushes.Gray);
    }

    private static FrameworkElement CreateFullItemRow(
        FullSaleReportItemModel item,
        double width)
    {
        var grid = CreateFullGrid(width);
        AddCell(grid, 0, item.ProductCode ?? "—");
        AddCell(grid, 1, item.Barcode ?? "—", fontSize: 8.5);
        var product = new StackPanel { Margin = new Thickness(4, 5, 4, 5) };
        product.Children.Add(Text(item.ProductName, 10.5, FontWeights.SemiBold));
        if (!string.IsNullOrWhiteSpace(item.Comment))
        {
            product.Children.Add(Text(item.Comment!, 9, FontWeights.Normal,
                foreground: Brushes.DimGray, margin: new Thickness(0, 3, 0, 0)));
        }
        Grid.SetColumn(product, 2);
        grid.Children.Add(product);
        AddCell(grid, 3, item.MeasurementUnitName ?? "—");
        AddCell(grid, 4, ReportFormatting.Quantity(item.Quantity), alignment: TextAlignment.Right);
        AddCell(grid, 5, ReportFormatting.Money(item.UnitPrice), alignment: TextAlignment.Right);
        AddCell(grid, 6, ReportFormatting.Money(item.LineTotal), true, TextAlignment.Right);
        return WrapRow(grid, new Thickness(0, 0, 0, 1), Brushes.LightGray);
    }

    private static Grid CreateFullGrid(double width)
    {
        var grid = new Grid { Width = width };
        var ratios = new[] { 0.10, 0.13, 0.33, 0.09, 0.12, 0.11, 0.12 };
        foreach (var ratio in ratios)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width * ratio) });
        }
        return grid;
    }

    private static FrameworkElement CreateFinancialSummary(
        FullSaleReportModel report,
        double width)
    {
        var outer = new Border
        {
            Width = width,
            Margin = new Thickness(0, 14, 0, 0),
            Padding = new Thickness(12),
            Background = new SolidColorBrush(Color.FromRgb(246, 248, 250)),
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1)
        };
        var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right, Width = 300 };
        panel.Children.Add(SummaryLine("პროდუქტების ჯამი", report.Subtotal, 12));
        panel.Children.Add(SummaryLine("ფასდაკლება", report.DiscountAmount, 12));
        panel.Children.Add(SummaryLine("გადასახდელი", report.TotalAmount, 16, FontWeights.Bold));
        panel.Children.Add(SummaryLine("გადახდილი", report.PaidAmount, 13, FontWeights.SemiBold));
        panel.Children.Add(SummaryLine("ვალი", report.OutstandingAmount, 13,
            report.OutstandingAmount > 0 ? FontWeights.Bold : FontWeights.Normal));
        panel.Children.Add(new Separator { Margin = new Thickness(0, 7, 0, 7) });
        panel.Children.Add(SummaryLine("ნაღდი", report.CashAmount, 10));
        panel.Children.Add(SummaryLine("ბარათი", report.CardAmount, 10));
        panel.Children.Add(SummaryLine("გადარიცხვა", report.BankTransferAmount, 10));
        panel.Children.Add(SummaryLine("სხვა", report.OtherAmount, 10));
        outer.Child = panel;
        return outer;
    }

    private static FrameworkElement CreateLoadingHeader(
        LoadingListReportModel report,
        double width)
    {
        var border = new Border
        {
            Width = width,
            Padding = new Thickness(0, 0, 0, 10),
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(0, 0, 0, 1)
        };
        var panel = new StackPanel();
        panel.Children.Add(Text("დასატვირთი პროდუქცია", 20, FontWeights.Bold,
            TextAlignment.Center));
        panel.Children.Add(Text($"გაყიდვა: {report.SaleNumber}", 12, FontWeights.SemiBold,
            margin: new Thickness(0, 8, 0, 0)));
        panel.Children.Add(Text($"მყიდველი: {report.CustomerName ?? "—"}", 11));
        panel.Children.Add(Text($"დაბეჭდილია: {report.PrintedAt:dd.MM.yyyy HH:mm}", 10));
        border.Child = panel;
        return border;
    }

    private static FrameworkElement CreatePrintComment(string comment, double width)
        => new Border
        {
            Width = width,
            Margin = new Thickness(0, 10, 0, 4),
            Padding = new Thickness(10),
            Background = new SolidColorBrush(Color.FromRgb(255, 248, 220)),
            BorderBrush = Brushes.Goldenrod,
            BorderThickness = new Thickness(1),
            Child = Text(comment, 11, FontWeights.SemiBold)
        };

    private static FrameworkElement CreateLoadingItem(
        LoadingListReportItemModel item,
        double width)
    {
        var border = new Border
        {
            Width = width,
            Margin = new Thickness(0, 7, 0, 0),
            Padding = new Thickness(11, 9, 11, 9),
            BorderBrush = new SolidColorBrush(Color.FromRgb(165, 172, 181)),
            BorderThickness = new Thickness(0, 0, 0, 1)
        };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
        var details = new StackPanel { Margin = new Thickness(0, 0, 14, 0) };
        details.Children.Add(Text(item.ProductName, 16, FontWeights.SemiBold));
        if (!string.IsNullOrWhiteSpace(item.ProductCode))
        {
            details.Children.Add(Text($"კოდი: {item.ProductCode}", 9.5, FontWeights.Normal,
                foreground: Brushes.DimGray, margin: new Thickness(0, 3, 0, 0)));
        }
        if (!string.IsNullOrWhiteSpace(item.Comment))
        {
            details.Children.Add(Text(item.Comment!, 10.5, FontWeights.Normal,
                margin: new Thickness(0, 5, 0, 0)));
        }
        grid.Children.Add(details);
        var quantity = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        quantity.Children.Add(Text(
            $"{ReportFormatting.Quantity(item.LoadingQuantity)} {item.MeasurementUnitName ?? string.Empty}".Trim(),
            19,
            FontWeights.Bold,
            TextAlignment.Right));
        Grid.SetColumn(quantity, 1);
        grid.Children.Add(quantity);
        border.Child = grid;
        return border;
    }

    private static Border WrapRow(Grid grid, Thickness margin, Brush borderBrush)
        => new()
        {
            Width = grid.Width,
            Margin = margin,
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = grid
        };

    private static void AddCell(
        Grid grid,
        int column,
        string value,
        bool bold = false,
        TextAlignment alignment = TextAlignment.Left,
        double fontSize = 9.5)
    {
        var text = Text(value, fontSize, bold ? FontWeights.SemiBold : FontWeights.Normal,
            alignment, margin: new Thickness(4, 5, 4, 5));
        text.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(text, column);
        grid.Children.Add(text);
    }

    private static FrameworkElement LabelValue(string label, string value)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(Text($"{label}: ", 10, FontWeights.SemiBold));
        panel.Children.Add(Text(value, 10, FontWeights.Normal));
        return panel;
    }

    private static FrameworkElement SummaryLine(
        string label,
        decimal amount,
        double size,
        FontWeight? weight = null)
    {
        var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        grid.Children.Add(Text(label, size, weight ?? FontWeights.Normal));
        var amountText = Text($"{ReportFormatting.Money(amount)} ₾", size,
            weight ?? FontWeights.Normal, TextAlignment.Right);
        Grid.SetColumn(amountText, 1);
        grid.Children.Add(amountText);
        return grid;
    }

    private static TextBlock Text(
        string value,
        double fontSize,
        FontWeight? weight = null,
        TextAlignment alignment = TextAlignment.Left,
        Brush? foreground = null,
        Thickness? margin = null)
        => new()
        {
            Text = value,
            FontFamily = GeorgianFont,
            FontSize = fontSize,
            FontWeight = weight ?? FontWeights.Normal,
            TextAlignment = alignment,
            TextWrapping = TextWrapping.Wrap,
            Foreground = foreground ?? Brushes.Black,
            Margin = margin ?? new Thickness(0)
        };

    private sealed class PageComposer
    {
        private const double FooterHeight = 24;
        private readonly Size _pageSize;
        private readonly FixedDocument _document = new();
        private readonly List<FixedPage> _pages = [];
        private FixedPage _currentPage = null!;
        private double _currentY;

        public PageComposer(Size pageSize)
        {
            _pageSize = pageSize;
            _document.DocumentPaginator.PageSize = pageSize;
        }

        public double ContentWidth => _pageSize.Width - (Margin * 2);
        private double ContentBottom => _pageSize.Height - Margin - FooterHeight;

        public void StartPage(
            Func<FrameworkElement> header,
            Func<FrameworkElement>? secondaryHeader = null)
        {
            _currentPage = new FixedPage
            {
                Width = _pageSize.Width,
                Height = _pageSize.Height,
                Background = Brushes.White
            };
            var pageContent = new PageContent { Child = _currentPage };
            _document.Pages.Add(pageContent);
            _pages.Add(_currentPage);
            _currentY = Margin;
            AddMeasured(header());
            if (secondaryHeader is not null)
            {
                AddMeasured(secondaryHeader());
            }
        }

        public void Add(
            Func<FrameworkElement> elementFactory,
            Func<FrameworkElement> header,
            Func<FrameworkElement>? secondaryHeader = null)
        {
            var element = elementFactory();
            var height = Measure(element);
            if (_currentY + height > ContentBottom && _currentY > Margin)
            {
                StartPage(header, secondaryHeader);
                element = elementFactory();
            }
            AddMeasured(element);
        }

        public FixedDocument Finish()
        {
            for (var index = 0; index < _pages.Count; index++)
            {
                var footer = Text(
                    $"გვერდი {index + 1} / {_pages.Count}",
                    9,
                    FontWeights.Normal,
                    TextAlignment.Center,
                    Brushes.DimGray);
                footer.Width = ContentWidth;
                footer.Measure(new Size(ContentWidth, FooterHeight));
                FixedPage.SetLeft(footer, Margin);
                FixedPage.SetTop(footer, _pageSize.Height - Margin);
                _pages[index].Children.Add(footer);
            }
            return _document;
        }

        private void AddMeasured(FrameworkElement element)
        {
            var height = Measure(element);
            FixedPage.SetLeft(element, Margin);
            FixedPage.SetTop(element, _currentY);
            _currentPage.Children.Add(element);
            _currentY += height;
        }

        private double Measure(FrameworkElement element)
        {
            element.Width = double.IsNaN(element.Width) ? ContentWidth : element.Width;
            element.Measure(new Size(ContentWidth, double.PositiveInfinity));
            element.Arrange(new Rect(0, 0, ContentWidth, element.DesiredSize.Height));
            return Math.Max(element.DesiredSize.Height, 1);
        }
    }
}
