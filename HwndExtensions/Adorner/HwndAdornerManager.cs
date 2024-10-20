using System;
using System.Windows.Controls;

namespace HwndExtensions.Adorner;

/// <summary>
/// Hwnd Adorner Manager
/// </summary>
public sealed class HwndAdornerManager : Decorator, IHwndAdornerManager, IDisposable
{
    private readonly HwndAdornerGroup _mHwndAdornerGroup;
    private bool _mDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HwndAdornerManager"/> class.
    /// </summary>
    public HwndAdornerManager()
    {
        _mHwndAdornerGroup = new HwndAdornerGroup(this);
    }

    /// <inheritdoc />
    HwndAdornerGroup IHwndAdornerManager.AdornerGroup
    {
        get
        {
            if (_mDisposed)
            {
                throw new ObjectDisposedException("this");
            }

            return _mHwndAdornerGroup;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _mDisposed = true;
        _mHwndAdornerGroup.Dispose();
    }
}