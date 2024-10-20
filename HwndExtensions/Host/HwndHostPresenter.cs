using System;
using System.Collections;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

using HwndExtensions.Adorner;
using HwndExtensions.Utils;

namespace HwndExtensions.Host;

/// <summary>
/// A custom control for managing an HwndHost child and presenting an Adornment over it.
/// Inherited classes must control the access and life cycle of the HwndHost child
/// </summary>
public class HwndHostPresenter : FrameworkElement, IDisposable
{
    // A property maintaining the Mouse Over state for all content - including an
    // HwndHost with a Message Loop on another thread
    // HwndHost childs should raise the HwndExtensions.HwndMouseXXX routed events

    /// <summary>
    /// DependencyProperty for <see cref="IsMouseOverOverride" /> property key.
    /// </summary>
    public static readonly DependencyPropertyKey IsMouseOverOverridePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsMouseOverOverride),
        typeof(bool),
        typeof(HwndHostPresenter),
        new PropertyMetadata(false));

    /// <summary>
    /// DependencyProperty for <see cref="Background" /> property.
    /// </summary>
    public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
        nameof(Background),
        typeof(Brush),
        typeof(HwndHostPresenter),
        new FrameworkPropertyMetadata(default, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

    /// <summary>
    /// DependencyProperty for <see cref="IsMouseOverOverride" /> property.
    /// </summary>
    public static readonly DependencyProperty IsMouseOverOverrideProperty = IsMouseOverOverridePropertyKey.DependencyProperty;

    /// <summary>
    /// DependencyProperty for <see cref="Hosting" /> property.
    /// </summary>
    public static readonly DependencyProperty HostingProperty = DependencyProperty.Register(
        nameof(Hosting), typeof(bool), typeof(HwndHostPresenter), new UIPropertyMetadata(true, OnHostingChanged));

    private readonly HwndAdorner _mHwndAdorner;
    private UIElement? _mChild;
    private UIElement? _mAdornment;
    private HwndHost? _mHwndHost;
    private bool _mMouseIsOverHwnd;

    static HwndHostPresenter()
    {
        EventManager.RegisterClassHandler(typeof(HwndHostPresenter), MouseEnterEvent, new RoutedEventHandler(OnMouseEnterOrLeave));
        EventManager.RegisterClassHandler(typeof(HwndHostPresenter), MouseLeaveEvent, new RoutedEventHandler(OnMouseEnterOrLeave));
        EventManager.RegisterClassHandler(typeof(HwndHostPresenter), HwndExtensions.HwndMouseEnterEvent, new RoutedEventHandler(OnHwndMouseEnterOrLeave));
        EventManager.RegisterClassHandler(typeof(HwndHostPresenter), HwndExtensions.HwndMouseLeaveEvent, new RoutedEventHandler(OnHwndMouseEnterOrLeave));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HwndHostPresenter"/> class.
    /// </summary>
    public HwndHostPresenter()
    {
        _mHwndAdorner = new HwndAdorner(this);

        AddLogicalChild(_mHwndAdorner.Root);
    }

    /// <summary>
    /// Gets or sets the HwndHost
    /// </summary>
    public HwndHost? HwndHost
    {
        get => _mHwndHost;
        set
        {
            if (_mHwndHost == value)
            {
                return;
            }

            RemoveLogicalChild(_mHwndHost);

            _mHwndHost = value;

            AddLogicalChild(value);

            if (Hosting)
            {
                Child = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the Adornment
    /// </summary>
    public UIElement Adornment
    {
        get => _mAdornment;
        set
        {
            if (_mAdornment == value)
            {
                return;
            }

            _mAdornment = value;
            _mHwndAdorner.Adornment = _mAdornment;
        }
    }

    /// <summary>
    /// The Background property defines the brush used to fill the area between borders.
    /// </summary>
    public Brush? Background
    {
        get => GetValue(BackgroundProperty) as Brush;
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>
    /// Gets IsMouseOverOverride
    /// </summary>
    public bool IsMouseOverOverride => (bool)GetValue(IsMouseOverOverrideProperty);

    /// <summary>
    /// Gets Hosting
    /// </summary>
    public bool Hosting
    {
        get => (bool)GetValue(HostingProperty);
        set => SetValue(HostingProperty, value);
    }

    /// <summary>
    /// The only visual child
    /// </summary>
    private UIElement? Child
    {
        get => _mChild;
        set
        {
            if (_mChild == value)
            {
                return;
            }

            RemoveVisualChild(_mChild);

            _mChild = value;

            AddVisualChild(value);
            InvalidateMeasure();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Inherited classes should decide whether to dispose the HwndHost child
    /// </summary>
    /// <param name="disposing">disposing</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            HwndHost?.Dispose();
        }
    }

    /// <summary>
    /// The Adorner Root is always a logical child
    /// so is The HwndHost if exists
    /// </summary>
    protected override IEnumerator LogicalChildren
    {
        get
        {
            if (_mHwndHost != null)
            {
                return new object[] { _mHwndHost, _mHwndAdorner.Root }.GetEnumerator();
            }

            return new SingleChildEnumerator(_mHwndAdorner.Root);
        }
    }

    /// <summary>
    /// Returns the Visual children count.
    /// </summary>
    protected override int VisualChildrenCount => _mChild is null ? 0 : 1;

    /// <inheritdoc />
    protected override Visual? GetVisualChild(int index)
    {
        if (_mChild == null || index != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, @"presenter has one child at the most");
        }

        return _mChild;
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size constraint)
    {
        var child = Child;

        if (child != null)
        {
            child.Measure(constraint);

            return child.DesiredSize;
        }

        return Size.Empty;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size arrangeSize)
    {
        var child = Child;

        child?.Arrange(new Rect(arrangeSize));

        return arrangeSize;
    }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext)
    {
        var background = Background;

        if (background != null)
        {
            // Using the Background brush, draw a rectangle that fills the
            // render bounds of the panel.
            drawingContext.DrawRectangle(background, null, new Rect(RenderSize));
        }

        base.OnRender(drawingContext);
    }

    /// <inheritdoc />
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        // openning context menu programmatically since it dosn't open when clicking above the HwndHost.
        // we raise the mouse event programmatically, so we can respond to it although the system dosn't
        if (e is { Handled: false, ChangedButton: MouseButton.Right } && ContextMenu != null)
        {
            ContextMenu.PlacementTarget = this; // important for receiving the correct DataContext
            ContextMenu.IsOpen = true;
            e.Handled = true;
        }

        base.OnMouseUp(e);
    }

    private static void OnHostingChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
    {
        var presenter = (HwndHostPresenter)d;
        presenter.OnHostingChanged((bool)args.NewValue);
    }

    private static void OnMouseEnterOrLeave(object sender, RoutedEventArgs e)
    {
        var presenter = sender as HwndHostPresenter;

        presenter?.InvalidateMouseOver();
    }

    private static void OnHwndMouseEnterOrLeave(object sender, RoutedEventArgs e)
    {
        if (sender is HwndHostPresenter presenter && ReferenceEquals(e.OriginalSource, presenter._mHwndHost))
        {
            // Handling this routed event only if its coming from our direct child
            presenter._mMouseIsOverHwnd = e.RoutedEvent == HwndExtensions.HwndMouseEnterEvent;
            presenter.InvalidateMouseOver();
        }
    }

    private void InvalidateMouseOver()
    {
        var over = IsMouseOver || (_mHwndHost != null && _mMouseIsOverHwnd);

        SetValue(IsMouseOverOverridePropertyKey, over);
    }

    private void OnHostingChanged(bool hosting)
    {
        Child = hosting ? _mHwndHost : default;
    }
}