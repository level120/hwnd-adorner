using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

using HwndExtensions.Utils;

namespace HwndExtensions.Adorner;

/// <summary>
/// A class for managing an adornment above all other content (including non-WPF child windows (hwnd), unlike the WPF Adorner classes)
/// </summary>
public sealed class HwndAdorner : IDisposable
{
    // See the HwndAdornerElement class for a simple usage example.
    //
    // Another way of using this class is through the HwndExtensions.HwndAdornment attached property,
    // which can attach any UIElement as an Adornment to any FrameworkElement.
    // This option lacks the logical parenting provided by HwndAdornerElement.
    //
    // Event routing should work in any case (through the GetUIParentCore override of the HwndAdornmentRoot class)
    private const uint NoRepositionFlags = Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE |
                                             Win32.SWP_NOZORDER | Win32.SWP_NOOWNERZORDER | Win32.SWP_NOREPOSITION;

    private const uint SetOnlyLocation = Win32.SWP_NOACTIVATE | Win32.SWP_NOZORDER | Win32.SWP_NOOWNERZORDER;

    private readonly FrameworkElement _mElementAttachedTo;
    private readonly HwndAdornmentRoot _mHwndAdornmentRoot;

    private HwndAdornerGroup? _mHwndAdornerGroup;
    private HwndSource? _mHwndSource;
    private UIElement _mAdornment;
    private Rect _mParentBoundingBox;
    private Rect _mBoundingBox;
    private bool _mShown;
    private bool _mDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HwndAdorner"/> class.
    /// </summary>
    /// <param name="attachedTo">attachedTo</param>
    public HwndAdorner(FrameworkElement attachedTo)
    {
        _mElementAttachedTo = attachedTo;
        _mParentBoundingBox = _mBoundingBox = new Rect(new Point(), Size.Empty);

        _mHwndAdornmentRoot = new HwndAdornmentRoot
        {
            UIParentCore = _mElementAttachedTo,
        };

        _mElementAttachedTo.Loaded += OnLoaded;
        _mElementAttachedTo.Unloaded += OnUnloaded;
        _mElementAttachedTo.IsVisibleChanged += OnIsVisibleChanged;
        _mElementAttachedTo.LayoutUpdated += OnLayoutUpdated;
    }

    /// <summary>
    /// Gets Root
    /// </summary>
    public FrameworkElement Root => _mHwndAdornmentRoot;

    /// <summary>
    /// Gets or sets Adornment
    /// </summary>
    public UIElement Adornment
    {
        get => _mAdornment;
        set
        {
            if (_mDisposed)
            {
                throw new ObjectDisposedException(nameof(HwndAdorner));
            }

            _mAdornment = value;

            if (_mElementAttachedTo.IsLoaded)
            {
                _mHwndAdornmentRoot.Content = _mAdornment;
            }
        }
    }

    /// <summary>
    /// Gets Handle
    /// </summary>
    internal IntPtr Handle => _mHwndSource?.Handle ?? IntPtr.Zero;

    /// <summary>
    /// Gets Owned
    /// </summary>
    private bool Owned => _mHwndAdornerGroup is { Owned: true };

    /// <summary>
    /// Gets Needs to Appear
    /// </summary>
    private bool NeedsToAppear => Owned && _mElementAttachedTo.IsVisible;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_mDisposed)
        {
            return;
        }

        DisconnectFromGroup();

        _mHwndAdornmentRoot.Content = null;

        DisposeHwndSource();

        _mElementAttachedTo.Loaded -= OnLoaded;
        _mElementAttachedTo.Unloaded -= OnUnloaded;
        _mElementAttachedTo.IsVisibleChanged -= OnIsVisibleChanged;
        _mElementAttachedTo.LayoutUpdated -= OnLayoutUpdated;

        _mDisposed = true;
    }

    /// <summary>
    /// Invalidate Appearance
    /// </summary>
    internal void InvalidateAppearance()
    {
        if (_mHwndSource == null)
        {
            return;
        }

        if (NeedsToAppear)
        {
            if (!_mShown)
            {
                Win32.SetWindowPos(_mHwndSource.Handle, IntPtr.Zero, 0, 0, 0, 0, NoRepositionFlags | Win32.SWP_SHOWWINDOW);
                _mShown = true;
            }
        }
        else if (_mShown)
        {
            Win32.SetWindowPos(_mHwndSource.Handle, IntPtr.Zero, 0, 0, 0, 0, NoRepositionFlags | Win32.SWP_HIDEWINDOW);
            _mShown = false;
        }
    }

    /// <summary>
    /// Update Owner Position
    /// </summary>
    /// <param name="rect">rect</param>
    internal void UpdateOwnerPosition(Rect rect)
    {
        if (!_mParentBoundingBox.Equals(rect))
        {
            _mParentBoundingBox = rect;

            SetAbsolutePosition();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        InitHwndSource();
        _mHwndAdornmentRoot.Content = _mAdornment;
        ConnectToGroup();
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        DisconnectFromGroup();
        _mHwndAdornmentRoot.Content = null;
        DisposeHwndSource();
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
    {
        InvalidateAppearance();
    }

    private void OnLayoutUpdated(object sender, EventArgs eventArgs)
    {
        var compositionTarget = default(CompositionTarget);
        var source = PresentationSource.FromVisual(_mElementAttachedTo);

        if (source != null)
        {
            compositionTarget = source.CompositionTarget;
        }

        if (compositionTarget is { RootVisual: not null })
        {
            UpdateBoundingBox(CalculateAssignedRC(source!));
        }
    }

    private void UpdateBoundingBox(Rect boundingBox)
    {
        if (!_mBoundingBox.Equals(boundingBox))
        {
            _mBoundingBox = boundingBox;
            SetAbsolutePosition();
        }
    }

    private Rect CalculateAssignedRC(PresentationSource source)
    {
        var rectElement = new Rect(_mElementAttachedTo.RenderSize);
        var rectRoot = RectUtil.ElementToRoot(rectElement, _mElementAttachedTo, source);

        return RectUtil.RootToClient(rectRoot, source);
    }

    private void ConnectToGroup()
    {
        DisconnectFromGroup();

        var manager = WpfTreeExtensions.TryFindVisualAncestor<IHwndAdornerManager>(_mElementAttachedTo);

        _mHwndAdornerGroup = manager == null ? new HwndAdornerGroup(_mElementAttachedTo) : manager.AdornerGroup;
        _mHwndAdornerGroup.AddAdorner(this);
    }

    private void DisconnectFromGroup()
    {
        if (_mHwndAdornerGroup == null)
        {
            return;
        }

        _mHwndAdornerGroup.RemoveAdorner(this);
        _mHwndAdornerGroup = null;
    }

    private void SetAbsolutePosition()
    {
        if (_mHwndSource == null)
        {
            return;
        }

        Win32.SetWindowPos(
            _mHwndSource.Handle,
            IntPtr.Zero,
            (int)(_mParentBoundingBox.X + _mBoundingBox.X),
            (int)(_mParentBoundingBox.Y + _mBoundingBox.Y),
            (int)Math.Min(_mBoundingBox.Width, _mParentBoundingBox.Width - _mBoundingBox.X),
            (int)Math.Min(_mBoundingBox.Height, _mParentBoundingBox.Height - _mBoundingBox.Y),
            SetOnlyLocation | Win32.SWP_ASYNCWINDOWPOS);
    }

    private void InitHwndSource()
    {
        if (_mHwndSource != null)
        {
            return;
        }

        var parameters = new HwndSourceParameters()
        {
            UsesPerPixelOpacity = true,
            WindowClassStyle = 0,
            WindowStyle = 0,
            ExtendedWindowStyle = Win32.WS_EX_NOACTIVATE,
            PositionX = (int)(_mParentBoundingBox.X + _mBoundingBox.X),
            PositionY = (int)(_mParentBoundingBox.Y + _mBoundingBox.Y),
            Width = (int)_mBoundingBox.Width,
            Height = (int)_mBoundingBox.Height,
        };

        _mHwndSource = new HwndSource(parameters);
        _mHwndSource.RootVisual = _mHwndAdornmentRoot;
        _mHwndSource.AddHook(WndProc);

        _mShown = false;
    }

    private void DisposeHwndSource()
    {
        if (_mHwndSource == null)
        {
            return;
        }

        _mHwndSource.RemoveHook(WndProc);
        _mHwndSource.Dispose();
        _mHwndSource = null;

        _mShown = false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32.WM_ACTIVATE)
        {
            if (Owned)
            {
                _mHwndAdornerGroup?.ActivateInGroupLimits(this);
            }
        }
        else if (msg == Win32.WM_GETMINMAXINFO)
        {
            unsafe
            {
                MINMAXINFO* minMaxInfo = (MINMAXINFO*)lParam;
                minMaxInfo->ptMinTrackSize = new POINT { X = 0, Y = 0 };
            }

            // A safe inefficient version for the unsafe block above

            // var minMaxInfo = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof (MINMAXINFO));
            // minMaxInfo.ptMinTrackSize = new POINT();
            // Marshal.StructureToPtr(minMaxInfo, lParam, true);
        }

        return IntPtr.Zero;
    }
}