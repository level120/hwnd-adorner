using System.Windows;
using System.Windows.Controls;

namespace HwndExtensions.Adorner;

/// <summary>
/// Hwnd Adornment Root
/// </summary>
internal class HwndAdornmentRoot : ContentControl
{
    /// <summary>
    /// UI Parent Core
    /// </summary>
    public DependencyObject? UIParentCore { get; set; }

    /// <inheritdoc />
    protected override DependencyObject? GetUIParentCore()
    {
        return UIParentCore ?? base.GetUIParentCore();
    }
}