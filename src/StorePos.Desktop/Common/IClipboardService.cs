namespace StorePos.Desktop.Common;

public interface IClipboardService
{
    bool TrySetText(string? text);
}
