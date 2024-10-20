using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using Microsoft.Xaml.Behaviors;

namespace HwndExtensions.Host;

/// <summary>
/// Extended HwndHost
/// </summary>
public abstract class ExtendedHwndHost : HwndHost, IHwndHolder
{
    private Rect _mLatestBounds;
    private Rect _mFreezedBounds;
    private bool _mBoundsAreFreezed;

    /// <summary>
    /// IsMouseOverHwnd Property
    /// </summary>
    public static readonly DependencyProperty IsMouseOverHwndProperty = IsMouseOverHwndPropertyKey.DependencyProperty;

    /// <summary>
    /// ConnectsToHostManager Property
    /// </summary>
    public static readonly DependencyProperty ConnectsToHostManagerProperty = DependencyProperty.Register(
        nameof(ConnectsToHostManager), typeof(bool), typeof(ExtendedHwndHost), new PropertyMetadata(false, OnConnectsToHostManagerChanged));

    /// <summary>
    /// IsMouseOverHwnd Property Key
    /// </summary>
    private static readonly DependencyPropertyKey IsMouseOverHwndPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsMouseOverHwnd),
        typeof(bool),
        typeof(ExtendedHwndHost),
        new PropertyMetadata(false));

    static ExtendedHwndHost()
    {
        EventManager.RegisterClassHandler(typeof(ExtendedHwndHost), HwndExtensions.HwndMouseEnterEvent, new MouseEventHandler(OnHwndMouseEnterOrLeave));
        EventManager.RegisterClassHandler(typeof(ExtendedHwndHost), HwndExtensions.HwndMouseLeaveEvent, new MouseEventHandler(OnHwndMouseEnterOrLeave));
    }

    /// <summary>
    /// Gets or sets the ConnectsToHostManager
    /// </summary>
    public bool ConnectsToHostManager
    {
        get => (bool)GetValue(ConnectsToHostManagerProperty);
        set => SetValue(ConnectsToHostManagerProperty, value);
    }

    /// <summary>
    /// Gets IsMouseOverHwnd
    /// </summary>
    public bool IsMouseOverHwnd => (bool)GetValue(IsMouseOverHwndProperty);

    /// <summary>
    /// Gets LatestHwndBounds
    /// </summary>
    public Rect LatestHwndBounds => _mLatestBounds;

    /// <summary>
    /// Gets FreezedHwndBounds
    /// </summary>
    public Rect FreezedHwndBounds => _mFreezedBounds;

    /// <summary>
    /// Collapsed Hwnd
    /// </summary>
    /// <param name="freezeBounds">freezeBounds</param>
    public void CollapseHwnd(bool freezeBounds = false)
    {
        if (_mBoundsAreFreezed)
        {
            return;
        }

        if (freezeBounds)
        {
            FreezeHwndBounds();
        }
        else
        {
            _mBoundsAreFreezed = false;
        }

        var collapsedBox = new Rect(_mLatestBounds.Location, Size.Empty);

        OnWindowPositionChangedOverride(collapsedBox);
    }

    /// <summary>
    /// Freeze Hwnd Bounds
    /// </summary>
    public void FreezeHwndBounds()
    {
        _mBoundsAreFreezed = true;
        _mFreezedBounds = _mLatestBounds;
    }

    /// <summary>
    /// Expand Hwnd
    /// </summary>
    public void ExpandHwnd()
    {
        _mBoundsAreFreezed = false;

        OnWindowPositionChangedOverride(_mLatestBounds);
    }

    /// <summary>
    /// Expand On Next Reposition
    /// </summary>
    public void ExpandOnNextReposition()
    {
        _mBoundsAreFreezed = false;
    }

    /// <summary>
    /// <see cref="OnWindowPositionChanged"/> Override
    /// </summary>
    /// <param name="rcBoundingBox">rcBoundingBox</param>
    protected virtual void OnWindowPositionChangedOverride(Rect rcBoundingBox)
    {
        base.OnWindowPositionChanged(rcBoundingBox);
    }

    /// <inheritdoc />
    protected sealed override void OnWindowPositionChanged(Rect rcBoundingBox)
    {
        _mLatestBounds = rcBoundingBox;

        if (_mBoundsAreFreezed)
        {
            return;
        }

        OnWindowPositionChangedOverride(rcBoundingBox);
    }

    private static void OnHwndMouseEnterOrLeave(object sender, MouseEventArgs e)
    {
        var host = (ExtendedHwndHost) sender;
        host.SetValue(IsMouseOverHwndPropertyKey, e.RoutedEvent == HwndExtensions.HwndMouseEnterEvent);
    }

    private static void OnConnectsToHostManagerChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs args)
    {
        var host = (ExtendedHwndHost)depObj;

        if ((bool)args.NewValue)
        {
            host.AttachConnectToHostManagerBehavior();
        }
        else
        {
            host.DettachConnectToHostManagerBehavior();
        }
    }

    private void AttachConnectToHostManagerBehavior()
    {
        var connectBehavior = SearchForConnectBehavior();

        if (connectBehavior == null)
        {
            Interaction.GetBehaviors(this).Add(new ConnectToHostManagerBehavior<ExtendedHwndHost>());
        }
    }

    private void DettachConnectToHostManagerBehavior()
    {
        var connectBehavior = SearchForConnectBehavior();

        if (connectBehavior != null)
        {
            Interaction.GetBehaviors(this).Remove(connectBehavior);
        }
    }

    private ConnectToHostManagerBehavior<ExtendedHwndHost>? SearchForConnectBehavior()
    {
        return Interaction.GetBehaviors(this)
            .OfType<ConnectToHostManagerBehavior<ExtendedHwndHost>>()
            .FirstOrDefault();
    }
}