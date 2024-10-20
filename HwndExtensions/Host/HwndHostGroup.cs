using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using HwndExtensions.Utils;

namespace HwndExtensions.Host;

/// <summary>
/// A class for managing positions for a group of Hwnd's.
/// </summary>
public class HwndHostGroup : IDisposable
{
    private readonly FrameworkElement _mConnector;
    private readonly List<IHwndHolder> _mHwndHolders;
    private readonly PointDistanceComparer _mComparer;
    private bool _mExpandingAsync;

    /// <summary>
    /// Initializes a new instance of the <see cref="HwndHostGroup"/> class.
    /// </summary>
    /// <param name="connector">connector</param>
    public HwndHostGroup(FrameworkElement connector)
    {
        _mConnector = connector;
        _mHwndHolders = new List<IHwndHolder>();
        _mComparer = new PointDistanceComparer();

        connector.Loaded += OnConnectorLoaded;
        connector.Unloaded += OnConnectorUnloaded;
    }

    /// <summary>
    /// Add Host
    /// </summary>
    /// <param name="hwndHolder">hwndHolder</param>
    public void AddHost(IHwndHolder hwndHolder)
    {
        if (hwndHolder == null)
        {
            throw new ArgumentNullException(nameof(hwndHolder));
        }

        if (!_mHwndHolders.Contains(hwndHolder))
        {
            _mHwndHolders.Add(hwndHolder);
        }

        if (!_mExpandingAsync)
        {
            // Making sure we dont accidently stay with a collapsed hwnd
            hwndHolder.ExpandHwnd();
        }
    }

    /// <summary>
    /// Remove Host
    /// </summary>
    /// <param name="hwndHolder">hwndHolder</param>
    public void RemoveHost(IHwndHolder hwndHolder)
    {
        if (hwndHolder == null)
        {
            throw new ArgumentNullException(nameof(hwndHolder));
        }

        _mHwndHolders.Remove(hwndHolder);
    }

    /// <summary>
    /// Collapse Hwnds
    /// </summary>
    /// <param name="freezeBounds">freezeBounds</param>
    public void CollapseHwnds(bool freezeBounds = false)
    {
        if (_mHwndHolders.Count == 0)
        {
            return;
        }

        var latestBounds = _mHwndHolders.Select(h => h.LatestHwndBounds)
                                        .Aggregate(Rect.Union);

        _mComparer.RelativeTo = latestBounds.BottomRight;

        foreach (var hostHolder in _mHwndHolders.OrderBy(h => h.LatestHwndBounds.BottomRight, _mComparer))
        {
            hostHolder.CollapseHwnd(freezeBounds);
        }
    }

    /// <summary>
    /// Freeze Hwnds Bounds
    /// </summary>
    public void FreezeHwndsBounds()
    {
        if (_mHwndHolders.Count == 0)
        {
            return;
        }

        foreach (var hostHolder in _mHwndHolders)
        {
            hostHolder.FreezeHwndBounds();
        }
    }

    /// <summary>
    /// Expand Hwnds
    /// </summary>
    public void ExpandHwnds()
    {
        if (_mHwndHolders.Count == 0)
        {
            return;
        }

        var latestBounds = _mHwndHolders.Select(h => h.LatestHwndBounds)
                                        .Aggregate(Rect.Union);

        _mComparer.RelativeTo = latestBounds.TopLeft;

        foreach (var hostHolder in _mHwndHolders.OrderBy(h => h.LatestHwndBounds.TopLeft, _mComparer))
        {
            hostHolder.ExpandHwnd();
        }
    }

    /// <summary>
    /// Refresh Hwnds Async
    /// </summary>
    public void RefreshHwndsAsync()
    {
        CollapseHwnds(true);
        ExpandHwndsAsync();
    }

    /// <summary>
    /// Expand Hwnds Async
    /// </summary>
    public void ExpandHwndsAsync()
    {
        if (_mExpandingAsync)
        {
            return;
        }

        _mExpandingAsync = true;

        DispatchUI.OnUIThreadAsync(
            () =>
            {
                ExpandHwnds();
                _mExpandingAsync = false;
            },
            DispatcherPriority.Input);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _mConnector.Loaded -= OnConnectorLoaded;
        _mConnector.Unloaded -= OnConnectorUnloaded;
    }

    private void OnConnectorLoaded(object sender, RoutedEventArgs routedEventArgs)
    {
        ExpandHwndsAsync();
    }

    private void OnConnectorUnloaded(object sender, RoutedEventArgs routedEventArgs)
    {
        CollapseHwnds(true);
    }

    private class PointDistanceComparer : IComparer<Point>
    {
        public Point RelativeTo { get; set; }

        public int Compare(Point p1, Point p2)
        {
            var p1Distance = Math.Sqrt(Math.Pow(p1.X - RelativeTo.X, 2) + Math.Pow(p1.Y - RelativeTo.Y, 2));
            var p2Distance = Math.Sqrt(Math.Pow(p2.X - RelativeTo.X, 2) + Math.Pow(p2.Y - RelativeTo.Y, 2));

            return p1Distance.CompareTo(p2Distance);
        }
    }
}