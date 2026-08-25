using System.Runtime.InteropServices;
using System.Windows;

namespace StorePos.Desktop.Common;

public sealed class WindowsClipboardService : IClipboardService
{
    public bool TrySetText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        try
        {
            Clipboard.SetText(text.Trim());
            return true;
        }
        catch (ExternalException)
        {
            return false;
        }
    }
}
