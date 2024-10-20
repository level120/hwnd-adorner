using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Interop;

using HwndExtensions.Utils;

namespace HwndExtensions.Adorner;

/// <summary>
/// An internal class for managing the connection of a group of HwndAdorner's to their owner window.
/// The HwndAdorner searches up the visual tree for an IHwndAdornerManager containing an instance of this group,
/// if an IHwndAdornerManager is not found it creates a group containing only itself
/// </summary>
internal class HwndAdornerGroup : HwndSourceConnector
{
    // This class manages its base class resources (HwndSourceConnector) on its own.
    // i.e. when appropriately used, it dos not need to be disposed.
    private const uint SetOnlyZorder = Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE;

    private readonly List<HwndAdorner> _mAdornersInGroup;

    private HwndSource? _mOwnerSource;
    private bool _mOwned;

    /// <summary>
    /// Initializes a new instance of the <see cref="HwndAdornerGroup"/> class.
    /// </summary>
    /// <param name="commonAncestor">commonAncestor</param>
    internal HwndAdornerGroup(UIElement commonAncestor)
        : base(commonAncestor)
    {
        _mAdornersInGroup = new List<HwndAdorner>();
    }

    /// <summary>
    /// Owned
    /// </summary>
    internal bool Owned => _mOwned;

    private bool HasAdorners => _mAdornersInGroup.Count > 0;

    /// <summary>
    /// Add Adorner
    /// </summary>
    /// <param name="adorner">adorner</param>
    /// <returns>result</returns>
    internal bool AddAdorner(HwndAdorner adorner)
    {
        if (!Activated)
        {
            Activate();
        }

        if (!_mAdornersInGroup.Contains(adorner))
        {
            _mAdornersInGroup.Add(adorner);
        }

        if (_mOwned)
        {
            SetOwnership(adorner);
            ActivateInGroupLimits(adorner);
            adorner.InvalidateAppearance();

            if (_mOwnerSource?.RootVisual is UIElement root)
            {
                adorner.UpdateOwnerPosition(GetRectFromRoot(root));
            }
        }

        return true;
    }

    /// <summary>
    /// Remove Adorner
    /// </summary>
    /// <param name="adorner">adorner</param>
    /// <returns>result</returns>
    internal bool RemoveAdorner(HwndAdorner adorner)
    {
        var res = _mAdornersInGroup.Remove(adorner);

        if (_mOwned)
        {
            RemoveOwnership(adorner);
            adorner.InvalidateAppearance();
        }

        if (!HasAdorners)
        {
            Deactivate();
        }

        return res;
    }

    /// <summary>
    /// Activate In Group Limits
    /// </summary>
    /// <param name="adorner">adorner</param>
    internal void ActivateInGroupLimits(HwndAdorner adorner)
    {
        if (!_mOwned || _mOwnerSource == null)
        {
            return;
        }

        var current = _mOwnerSource.Handle;

        // getting the hwnd above the owner (in win32, the prev hwnd is the one visually above)
        var prev = Win32.GetWindow(current, Win32.GW_HWNDPREV);

        // searching up for the first non-sibling hwnd
        while (IsSibling(prev))
        {
            current = prev;
            prev = Win32.GetWindow(current, Win32.GW_HWNDPREV);
        }

        // the owner or one of the siblings is the Top-most window
        if (prev == IntPtr.Zero)
        {
            // setting the Top-most under the activated adorner
            Win32.SetWindowPos(current, adorner.Handle, 0, 0, 0, 0, SetOnlyZorder);
        }
        else
        {
            // setting the activated adorner under the first non-sibling hwnd
            Win32.SetWindowPos(adorner.Handle, prev, 0, 0, 0, 0, SetOnlyZorder);
        }
    }

    /// <inheritdoc />
    protected override void OnSourceConnected(HwndSource connectedSource)
    {
        if (_mOwned)
        {
            DisconnectFromOwner();
        }

        _mOwnerSource = connectedSource;
        _mOwnerSource.AddHook(OwnerHook);
        _mOwned = true;

        if (HasAdorners)
        {
            SetOwnership();
            SetZOrder();
            SetPosition();
            InvalidateAppearance();
        }
    }

    /// <inheritdoc />
    protected override void OnSourceDisconnected(HwndSource disconnectedSource)
    {
        DisconnectFromOwner();
    }

    private static Rect GetRectFromRoot(UIElement root)
    {
        return new Rect(
            root.PointToScreen(new Point()),
            root.PointToScreen(new Point(root.RenderSize.Width, root.RenderSize.Height)));
    }

    private static void RemoveOwnership(HwndAdorner adorner)
    {
        Win32.SetWindowLong(adorner.Handle, Win32.GWL_HWNDPARENT, IntPtr.Zero);
    }

    private void DisconnectFromOwner()
    {
        if (_mOwned)
        {
            _mOwnerSource?.RemoveHook(OwnerHook);
            _mOwnerSource = null;
            _mOwned = false;

            RemoveOwnership();
            InvalidateAppearance();
        }
    }

    private void SetOwnership()
    {
        foreach (var adorner in _mAdornersInGroup)
        {
            SetOwnership(adorner);
        }
    }

    private void InvalidateAppearance()
    {
        foreach (var adorner in _mAdornersInGroup)
        {
            adorner.InvalidateAppearance();
        }
    }

    private void SetOwnership(HwndAdorner adorner)
    {
        if (_mOwnerSource != null)
        {
            Win32.SetWindowLong(adorner.Handle, Win32.GWL_HWNDPARENT, _mOwnerSource.Handle);
        }
    }

    private void RemoveOwnership()
    {
        foreach (var adorner in _mAdornersInGroup)
        {
            RemoveOwnership(adorner);
        }
    }

    private void SetPosition()
    {
        if (_mOwnerSource?.RootVisual is not UIElement root)
        {
            return;
        }

        var rect = GetRectFromRoot(root);

        foreach (var adorner in _mAdornersInGroup)
        {
            adorner.UpdateOwnerPosition(rect);
        }
    }

    private void SetZOrder()
    {
        if (_mOwnerSource == null)
        {
            return;
        }

        // getting the hwnd above the owner (in win32, the prev hwnd is the one visually above)
        var hwndAbove = Win32.GetWindow(_mOwnerSource.Handle, Win32.GW_HWNDPREV);

        // owner is the Top most window
        if (hwndAbove == IntPtr.Zero && HasAdorners)
        {
            // randomly selecting an owned hwnd
            var owned = _mAdornersInGroup.First().Handle;

            // setting owner after (visually under) it
            Win32.SetWindowPos(_mOwnerSource.Handle, owned, 0, 0, 0, 0, SetOnlyZorder);

            // now this is the 'above' hwnd
            hwndAbove = owned;
        }

        // inserting all adorners between the owner and the hwnd initially above it
        // currently not preserving any previous z-order state between the adorners (unsupported for now)
        foreach (var adorner in _mAdornersInGroup)
        {
            var handle = adorner.Handle;

            Win32.SetWindowPos(handle, hwndAbove, 0, 0, 0, 0, SetOnlyZorder);
            hwndAbove = handle;
        }
    }

    private bool IsSibling(IntPtr hwnd)
    {
        return _mAdornersInGroup.Exists(o => o.Handle == hwnd);
    }

    private IntPtr OwnerHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32.WM_WINDOWPOSCHANGED)
        {
            SetPosition();
        }

        return IntPtr.Zero;
    }
}