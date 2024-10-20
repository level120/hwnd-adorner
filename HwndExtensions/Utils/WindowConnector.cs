using System.Windows;
using System.Windows.Interop;

namespace HwndExtensions.Utils;

/// <summary>
/// Window Connector
/// </summary>
public abstract class WindowConnector : HwndSourceConnector
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WindowConnector"/> class.
    /// </summary>
    /// <param name="connector">connector</param>
    protected WindowConnector(UIElement connector)
        : base(connector)
    {
    }

    /// <summary>
    /// OnWindowDisconnected
    /// </summary>
    /// <param name="disconnectedWindow">disconnectedWindow</param>
    protected abstract void OnWindowDisconnected(Window disconnectedWindow);

    /// <summary>
    /// OnWindowConnected
    /// </summary>
    /// <param name="connectedWindow">connectedWindow</param>
    protected abstract void OnWindowConnected(Window connectedWindow);

    /// <inheritdoc />
    protected sealed override void OnSourceConnected(HwndSource connectedSource)
    {
        if (connectedSource.RootVisual is Window window)
        {
            OnWindowConnected(window);
        }
    }

    /// <inheritdoc />
    protected sealed override void OnSourceDisconnected(HwndSource disconnectedSource)
    {
        if (disconnectedSource.RootVisual is Window window)
        {
            OnWindowDisconnected(window);
        }
    }
}