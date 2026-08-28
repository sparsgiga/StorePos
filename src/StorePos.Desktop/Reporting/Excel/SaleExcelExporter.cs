using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using StorePos.Desktop.Reporting.Models;

namespace StorePos.Desktop.Reporting.Excel;

public sealed class SaleExcelExporter
{
    private const string SpreadsheetNamespace =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string RelationshipNamespace =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public async Task ExportAsync(
        string filePath,
        FullSaleReportModel report,
        CancellationToken cancellationToken = default)
    {
        var workbook = await Task.Run(() => CreateWorkbook(report), cancellationToken);
        await File.WriteAllBytesAsync(filePath, workbook, cancellationToken);
    }

    public byte[] CreateWorkbook(FullSaleReportModel report)
    {
        ArgumentNullException.ThrowIfNull(report);

        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", WriteContentTypes);
            WriteEntry(archive, "_rels/.rels", WriteRootRelationships);
            WriteEntry(archive, "xl/workbook.xml", WriteWorkbook);
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", WriteWorkbookRelationships);
            WriteEntry(archive, "xl/styles.xml", WriteStyles);
            WriteEntry(
                archive,
                "xl/worksheets/sheet1.xml",
                writer => WriteWorksheet(writer, report));
        }

        return output.ToArray();
    }

    private static void WriteEntry(
        ZipArchive archive,
        string path,
        Action<XmlWriter> write)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var stream = entry.Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = false,
            CloseOutput = false
        });
        write(writer);
    }

    private static void WriteContentTypes(XmlWriter writer)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement("Types", "http://schemas.openxmlformats.org/package/2006/content-types");
        WriteEmpty(writer, "Default", ("Extension", "rels"),
            ("ContentType", "application/vnd.openxmlformats-package.relationships+xml"));
        WriteEmpty(writer, "Default", ("Extension", "xml"), ("ContentType", "application/xml"));
        WriteEmpty(writer, "Override", ("PartName", "/xl/workbook.xml"),
            ("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"));
        WriteEmpty(writer, "Override", ("PartName", "/xl/worksheets/sheet1.xml"),
            ("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"));
        WriteEmpty(writer, "Override", ("PartName", "/xl/styles.xml"),
            ("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"));
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteRootRelationships(XmlWriter writer)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");
        WriteEmpty(writer, "Relationship", ("Id", "rId1"),
            ("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
            ("Target", "xl/workbook.xml"));
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteWorkbook(XmlWriter writer)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement("workbook", SpreadsheetNamespace);
        writer.WriteAttributeString("xmlns", "r", null, RelationshipNamespace);
        writer.WriteStartElement("sheets", SpreadsheetNamespace);
        writer.WriteStartElement("sheet", SpreadsheetNamespace);
        writer.WriteAttributeString("name", "Sale Report");
        writer.WriteAttributeString("sheetId", "1");
        writer.WriteAttributeString("r", "id", RelationshipNamespace, "rId1");
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteWorkbookRelationships(XmlWriter writer)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");
        WriteEmpty(writer, "Relationship", ("Id", "rId1"),
            ("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
            ("Target", "worksheets/sheet1.xml"));
        WriteEmpty(writer, "Relationship", ("Id", "rId2"),
            ("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"),
            ("Target", "styles.xml"));
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteStyles(XmlWriter writer)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement("styleSheet", SpreadsheetNamespace);
        writer.WriteStartElement("numFmts");
        writer.WriteAttributeString("count", "1");
        WriteEmpty(writer, "numFmt", ("numFmtId", "164"), ("formatCode", "0.#####"));
        writer.WriteEndElement();
        writer.WriteStartElement("fonts");
        writer.WriteAttributeString("count", "3");
        writer.WriteStartElement("font");
        WriteEmpty(writer, "sz", ("val", "11"));
        WriteEmpty(writer, "name", ("val", "Sylfaen"));
        writer.WriteEndElement();
        writer.WriteStartElement("font");
        WriteEmpty(writer, "b");
        WriteEmpty(writer, "sz", ("val", "11"));
        WriteEmpty(writer, "name", ("val", "Sylfaen"));
        writer.WriteEndElement();
        writer.WriteStartElement("font");
        WriteEmpty(writer, "b");
        WriteEmpty(writer, "color", ("rgb", "FFFFFFFF"));
        WriteEmpty(writer, "sz", ("val", "11"));
        WriteEmpty(writer, "name", ("val", "Sylfaen"));
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteStartElement("fills");
        writer.WriteAttributeString("count", "3");
        writer.WriteStartElement("fill");
        WriteEmpty(writer, "patternFill", ("patternType", "none"));
        writer.WriteEndElement();
        writer.WriteStartElement("fill");
        WriteEmpty(writer, "patternFill", ("patternType", "gray125"));
        writer.WriteEndElement();
        writer.WriteStartElement("fill");
        writer.WriteStartElement("patternFill");
        writer.WriteAttributeString("patternType", "solid");
        WriteEmpty(writer, "fgColor", ("rgb", "FF1F4E78"));
        WriteEmpty(writer, "bgColor", ("indexed", "64"));
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteStartElement("borders");
        writer.WriteAttributeString("count", "2");
        writer.WriteStartElement("border");
        WriteEmpty(writer, "left");
        WriteEmpty(writer, "right");
        WriteEmpty(writer, "top");
        WriteEmpty(writer, "bottom");
        WriteEmpty(writer, "diagonal");
        writer.WriteEndElement();
        writer.WriteStartElement("border");
        WriteBorderSide(writer, "left");
        WriteBorderSide(writer, "right");
        WriteBorderSide(writer, "top");
        WriteBorderSide(writer, "bottom");
        WriteEmpty(writer, "diagonal");
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteStartElement("cellStyleXfs");
        writer.WriteAttributeString("count", "1");
        WriteEmpty(writer, "xf", ("numFmtId", "0"), ("fontId", "0"), ("fillId", "0"), ("borderId", "0"));
        writer.WriteEndElement();
        writer.WriteStartElement("cellXfs");
        writer.WriteAttributeString("count", "12");
        WriteXf(writer, 0, 0, false, false);
        WriteXf(writer, 0, 1, false, false);
        WriteXf(writer, 4, 0, true, false);
        WriteXf(writer, 164, 0, true, false);
        WriteXf(writer, 0, 0, false, true);
        WriteXf(writer, 0, 1, false, true);
        WriteXf(writer, 0, 2, false, false, fillId: 2, borderId: 1, centered: true);
        WriteXf(writer, 0, 0, false, false, borderId: 1);
        WriteXf(writer, 0, 0, false, true, borderId: 1);
        WriteXf(writer, 164, 0, true, false, borderId: 1);
        WriteXf(writer, 164, 0, true, false, borderId: 1);
        WriteXf(writer, 4, 0, true, false, borderId: 1);
        writer.WriteEndElement();
        writer.WriteStartElement("cellStyles");
        writer.WriteAttributeString("count", "1");
        WriteEmpty(writer, "cellStyle", ("name", "Normal"), ("xfId", "0"), ("builtinId", "0"));
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteXf(
        XmlWriter writer,
        int numberFormatId,
        int fontId,
        bool applyNumberFormat,
        bool wrap,
        int fillId = 0,
        int borderId = 0,
        bool centered = false)
    {
        writer.WriteStartElement("xf");
        writer.WriteAttributeString("numFmtId", numberFormatId.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("fontId", fontId.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("fillId", fillId.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("borderId", borderId.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("xfId", "0");
        if (applyNumberFormat)
        {
            writer.WriteAttributeString("applyNumberFormat", "1");
        }
        if (wrap || centered)
        {
            writer.WriteAttributeString("applyAlignment", "1");
            var attributes = new List<(string Name, string Value)> { ("vertical", "top") };
            if (wrap)
            {
                attributes.Add(("wrapText", "1"));
            }
            if (centered)
            {
                attributes.Add(("horizontal", "center"));
            }
            WriteEmpty(writer, "alignment", attributes.ToArray());
        }
        writer.WriteEndElement();
    }

    private static void WriteBorderSide(XmlWriter writer, string name)
    {
        writer.WriteStartElement(name);
        writer.WriteAttributeString("style", "thin");
        WriteEmpty(writer, "color", ("rgb", "FFB7C4CE"));
        writer.WriteEndElement();
    }

    private static void WriteWorksheet(XmlWriter writer, FullSaleReportModel report)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement("worksheet", SpreadsheetNamespace);
        writer.WriteStartElement("sheetViews");
        writer.WriteStartElement("sheetView");
        writer.WriteAttributeString("workbookViewId", "0");
        WriteEmpty(writer, "pane", ("ySplit", "1"), ("topLeftCell", "A2"),
            ("activePane", "bottomLeft"), ("state", "frozen"));
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteStartElement("cols");
        WriteColumn(writer, 1, 14);
        WriteColumn(writer, 2, 19);
        WriteColumn(writer, 3, 46);
        WriteColumn(writer, 4, 12);
        WriteColumn(writer, 5, 12);
        WriteColumn(writer, 6, 15);
        WriteColumn(writer, 7, 14);
        WriteColumn(writer, 8, 36);
        writer.WriteEndElement();
        writer.WriteStartElement("sheetData");

        WriteStringRow(writer, 1, 6,
            ("A", "კოდი"), ("B", "შტრიხკოდი"), ("C", "პროდუქტი"),
            ("D", "ერთეული"), ("E", "რაოდენობა"), ("F", "ერთ. ფასი"),
            ("G", "ჯამი"), ("H", "კომენტარი"));

        var rowNumber = 2;
        foreach (var item in report.Items)
        {
            WriteItemRow(writer, rowNumber++, item);
        }

        var lastItemRow = Math.Max(1, rowNumber - 1);

        rowNumber++;
        WriteMetadataRow(writer, rowNumber++,
            ("A", "გაყიდვა", "B", report.SaleNumber),
            ("C", "სტატუსი", "D", ReportFormatting.Status(report.Status)));
        WriteMetadataRow(writer, rowNumber++,
            ("A", "მყიდველი", "B", report.CustomerName ?? "—"),
            ("C", "საიდ. №", "D", report.CustomerIdentificationNumber ?? "—"));
        WriteMetadataRow(writer, rowNumber++,
            ("A", "შექმნილია", "B", report.DateCreated.ToString("dd.MM.yyyy HH:mm")),
            ("C", "დასრულებულია", "D", report.DateCompleted?.ToString("dd.MM.yyyy HH:mm") ?? "—"));
        WriteMetadataRow(writer, rowNumber++,
            ("A", "გაუქმებულია", "B", report.DateCancelled?.ToString("dd.MM.yyyy HH:mm") ?? "—"),
            ("C", "ექსპორტირებულია", "D", report.PrintedAt.ToString("dd.MM.yyyy HH:mm")));
        WriteMetadataRow(writer, rowNumber++,
            ("A", "კომენტარი", "B", report.Comment ?? "—"));

        WriteSummaryRow(writer, rowNumber++, "პროდუქტების ჯამი", report.Subtotal);
        WriteSummaryRow(writer, rowNumber++, "ფასდაკლება", report.DiscountAmount);
        WriteSummaryRow(writer, rowNumber++, "გადასახდელი", report.TotalAmount);
        WriteSummaryRow(writer, rowNumber++, "გადახდილი", report.PaidAmount);
        WriteSummaryRow(writer, rowNumber++, "ვალი", report.OutstandingAmount);
        WriteSummaryRow(writer, rowNumber++, "ნაღდი", report.CashAmount);
        WriteSummaryRow(writer, rowNumber++, "ბარათი", report.CardAmount);
        WriteSummaryRow(writer, rowNumber++, "გადარიცხვა", report.BankTransferAmount);
        WriteSummaryRow(writer, rowNumber, "სხვა", report.OtherAmount);

        writer.WriteEndElement();
        WriteEmpty(writer, "autoFilter", ("ref", $"A1:H{lastItemRow}"));
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteItemRow(
        XmlWriter writer,
        int row,
        FullSaleReportItemModel item)
    {
        writer.WriteStartElement("row");
        writer.WriteAttributeString("r", row.ToString(CultureInfo.InvariantCulture));
        WriteInlineCell(writer, $"A{row}", item.ProductCode ?? string.Empty, 7);
        WriteInlineCell(writer, $"B{row}", item.Barcode ?? string.Empty, 7);
        WriteInlineCell(writer, $"C{row}", item.ProductName, 8);
        WriteInlineCell(writer, $"D{row}", item.MeasurementUnitName ?? string.Empty, 7);
        WriteNumberCell(writer, $"E{row}", item.Quantity, 9);
        WriteNumberCell(writer, $"F{row}", item.UnitPrice, 10);
        WriteNumberCell(writer, $"G{row}", item.LineTotal, 11);
        WriteInlineCell(writer, $"H{row}", item.Comment ?? string.Empty, 8);
        writer.WriteEndElement();
    }

    private static void WriteSummaryRow(
        XmlWriter writer,
        int row,
        string label,
        decimal amount)
    {
        writer.WriteStartElement("row");
        writer.WriteAttributeString("r", row.ToString(CultureInfo.InvariantCulture));
        WriteInlineCell(writer, $"F{row}", label, 1);
        WriteNumberCell(writer, $"G{row}", amount, 2);
        writer.WriteEndElement();
    }

    private static void WriteStringRow(
        XmlWriter writer,
        int row,
        int style,
        params (string Column, string Value)[] cells)
    {
        writer.WriteStartElement("row");
        writer.WriteAttributeString("r", row.ToString(CultureInfo.InvariantCulture));
        foreach (var cell in cells)
        {
            WriteInlineCell(writer, $"{cell.Column}{row}", cell.Value, style);
        }
        writer.WriteEndElement();
    }

    private static void WriteMetadataRow(
        XmlWriter writer,
        int row,
        params (string LabelColumn, string Label, string ValueColumn, string Value)[] pairs)
    {
        writer.WriteStartElement("row");
        writer.WriteAttributeString("r", row.ToString(CultureInfo.InvariantCulture));
        foreach (var pair in pairs)
        {
            WriteInlineCell(writer, $"{pair.LabelColumn}{row}", pair.Label, 0);
            WriteInlineCell(writer, $"{pair.ValueColumn}{row}", pair.Value, 4);
        }
        writer.WriteEndElement();
    }

    private static void WriteInlineCell(
        XmlWriter writer,
        string reference,
        string value,
        int style)
    {
        writer.WriteStartElement("c");
        writer.WriteAttributeString("r", reference);
        writer.WriteAttributeString("t", "inlineStr");
        writer.WriteAttributeString("s", style.ToString(CultureInfo.InvariantCulture));
        writer.WriteStartElement("is");
        writer.WriteElementString("t", value);
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteNumberCell(
        XmlWriter writer,
        string reference,
        decimal value,
        int style)
    {
        writer.WriteStartElement("c");
        writer.WriteAttributeString("r", reference);
        writer.WriteAttributeString("s", style.ToString(CultureInfo.InvariantCulture));
        writer.WriteElementString("v", value.ToString(CultureInfo.InvariantCulture));
        writer.WriteEndElement();
    }

    private static void WriteColumn(XmlWriter writer, int index, double width)
        => WriteEmpty(
            writer,
            "col",
            ("min", index.ToString(CultureInfo.InvariantCulture)),
            ("max", index.ToString(CultureInfo.InvariantCulture)),
            ("width", width.ToString(CultureInfo.InvariantCulture)),
            ("customWidth", "1"));

    private static void WriteEmpty(
        XmlWriter writer,
        string elementName,
        params (string Name, string Value)[] attributes)
    {
        writer.WriteStartElement(elementName);
        foreach (var attribute in attributes)
        {
            writer.WriteAttributeString(attribute.Name, attribute.Value);
        }
        writer.WriteEndElement();
    }
}
