using System;
using System.Windows.Controls;

namespace HwndExtensions.Host;

/// <summary>
/// Hwnd Host Manager
/// </summary>
public sealed class HwndHostManager : Decorator, IHwndHostManager, IDisposable
{
    private readonly HwndHostGroup _mHostGroup;
    private bool _mDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HwndHostManager"/> class.
    /// </summary>
    public HwndHostManager()
    {
        _mHostGroup = new HwndHostGroup(this);
    }

    /// <summary>
    /// Hwnd Host Group
    /// </summary>
    public HwndHostGroup HwndHostGroup
    {
        get
        {
            if (_mDisposed)
            {
                throw new ObjectDisposedException("this");
            }

            return _mHostGroup;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_mDisposed)
        {
            _mDisposed = true;
            _mHostGroup.Dispose();
        }
    }
}