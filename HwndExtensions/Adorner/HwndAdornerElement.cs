using System.Collections;
using System.Windows;

using HwndExtensions.Utils;

namespace HwndExtensions.Adorner;

/// <summary>
/// Hwnd Adorner Element
/// </summary>
internal class HwndAdornerElement : FrameworkElement
{
    private readonly HwndAdorner _mHwndAdorner;

    /// <summary>
    /// Initializes a new instance of the <see cref="HwndAdornerElement"/> class.
    /// </summary>
    public HwndAdornerElement()
    {
        _mHwndAdorner = new HwndAdorner(this);

        // This helps dependency property inheritance and resource search cross the visual tree boundary
        // (between the tree containing this object and the one containing the adorner root)
        AddLogicalChild(_mHwndAdorner.Root);
    }

    /// <summary>
    /// Adornment
    /// </summary>
    public UIElement Adornment
    {
        get => _mHwndAdorner.Adornment;
        set => _mHwndAdorner.Adornment = value;
    }

    /// <inheritdoc />
    protected override IEnumerator LogicalChildren => new SingleChildEnumerator(_mHwndAdorner.Root);
}