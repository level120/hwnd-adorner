using System.Windows;
using HwndExtensions.Utils;

using Microsoft.Xaml.Behaviors;

namespace HwndExtensions.Host;

/// <summary>
/// Connect to Host Manager Behavior
/// </summary>
/// <typeparam name="T">Type</typeparam>
public class ConnectToHostManagerBehavior<T> : Behavior<T>
    where T : FrameworkElement, IHwndHolder
{
    private IHwndHostManager? _mHostManager;

    /// <inheritdoc />
    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.Loaded += OnLoaded;
        AssociatedObject.Unloaded += OnUnloaded;
    }

    /// <inheritdoc />
    protected override void OnDetaching()
    {
        DisconnectFromManager();

        AssociatedObject.Loaded -= OnLoaded;
        AssociatedObject.Unloaded -= OnUnloaded;

        base.OnDetaching();
    }

    private void ConnectToManager(IHwndHostManager? manager)
    {
        if (manager != null)
        {
            _mHostManager = manager;
            _mHostManager.HwndHostGroup.AddHost(AssociatedObject);
        }
        else
        {
            _mHostManager = null;
        }
    }

    private void DisconnectFromManager()
    {
        if (_mHostManager != null)
        {
            _mHostManager.HwndHostGroup.RemoveHost(AssociatedObject);
            _mHostManager = null;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
    {
        var manager = WpfTreeExtensions.TryFindVisualAncestor<IHwndHostManager>(AssociatedObject);

        if (_mHostManager != manager)
        {
            DisconnectFromManager();
            ConnectToManager(manager);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs routedEventArgs)
    {
        DisconnectFromManager();
    }
}