using System.Windows;
using System.Windows.Input;

using HwndExtensions.Adorner;

namespace HwndExtensions;

/// <summary>
/// Hwnd Extensions
/// </summary>
public static class HwndExtensions
{
    /// <summary>
    /// Mouse Enter
    /// </summary>
    public static readonly RoutedEvent HwndMouseEnterEvent = EventManager.RegisterRoutedEvent(
        "HwndMouseEnter",
        RoutingStrategy.Bubble,
        typeof(MouseEventHandler),
        typeof(HwndExtensions));

    /// <summary>
    /// Mouse Leave
    /// </summary>
    public static readonly RoutedEvent HwndMouseLeaveEvent = EventManager.RegisterRoutedEvent(
        "HwndMouseLeave",
        RoutingStrategy.Bubble,
        typeof(MouseEventHandler),
        typeof(HwndExtensions));

    /// <summary>
    /// Adornment
    /// </summary>
    public static readonly DependencyProperty HwndAdornmentProperty = DependencyProperty.RegisterAttached(
        "HwndAdornment", typeof(UIElement), typeof(HwndExtensions), new UIPropertyMetadata(null, OnAdornmentAttached));

    private static readonly DependencyProperty HwndAdornerProperty = DependencyProperty.RegisterAttached(
        "HwndAdorner", typeof(HwndAdorner), typeof(HwndExtensions), new PropertyMetadata(null));

    public static void SetHwndAdornment(DependencyObject? element, UIElement? value)
    {
        element?.SetValue(HwndAdornmentProperty, value);
    }

    public static UIElement? GetHwndAdornment(DependencyObject? element)
    {
        return element?.GetValue(HwndAdornmentProperty) as UIElement;
    }

    private static void SetHwndAdorner(DependencyObject element, HwndAdorner? value)
    {
        element.SetValue(HwndAdornerProperty, value);
    }

    private static HwndAdorner? GetHwndAdorner(DependencyObject element)
    {
        return element.GetValue(HwndAdornerProperty) as HwndAdorner;
    }

    private static void OnAdornmentAttached(DependencyObject d, DependencyPropertyChangedEventArgs args)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        var adorner = GetHwndAdorner(element);

        if (args.NewValue is UIElement adornment)
        {
            if (adorner == null)
            {
                SetHwndAdorner(element, adorner = new HwndAdorner(element));
            }

            adorner.Adornment = adornment;
        }
        else if (adorner != null)
        {
            adorner.Dispose();

            SetHwndAdorner(element, default);
        }
    }
}