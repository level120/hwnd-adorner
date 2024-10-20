using System;
using System.Windows;
using System.Windows.Interop;

namespace HwndExtensions.Utils;

/// <summary>
/// A class for managing the connection of an UIElement to its HwndSouce container.
/// Notifying on any HwndSouce change.
/// </summary>
public abstract class HwndSourceConnector : IDisposable
{
    private readonly UIElement _mConnector;

    /// <summary>
    /// Initializes a new instance of the <see cref="HwndSourceConnector"/> class.
    /// </summary>
    /// <param name="connector">connector</param>
    protected HwndSourceConnector(UIElement connector)
    {
        _mConnector = connector;
    }

    /// <summary>
    /// Activated
    /// </summary>
    protected bool Activated { get; private set; }

    /// <inheritdoc cref="Dispose"/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// OnSourceDisconnected
    /// </summary>
    /// <param name="disconnectedSource">disconnectedSource</param>
    protected abstract void OnSourceDisconnected(HwndSource disconnectedSource);

    /// <summary>
    /// OnSourceConnected
    /// </summary>
    /// <param name="connectedSource">connectedSource</param>
    protected abstract void OnSourceConnected(HwndSource connectedSource);

    /// <inheritdoc cref="Dispose(bool)"/>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Deactivate();
        }
    }

    /// <summary>
    /// Activate
    /// </summary>
    protected void Activate()
    {
        if (Activated)
        {
            return;
        }

        if (PresentationSource.FromVisual(_mConnector) is HwndSource hwndSource)
        {
            OnSourceConnected(hwndSource);
        }

        PresentationSource.AddSourceChangedHandler(_mConnector, OnSourceChanged);

        Activated = true;
    }

    /// <summary>
    /// Deactivate
    /// </summary>
    protected void Deactivate()
    {
        if (!Activated)
        {
            return;
        }

        if (PresentationSource.FromVisual(_mConnector) is HwndSource hwndSource)
        {
            OnSourceDisconnected(hwndSource);
        }

        PresentationSource.RemoveSourceChangedHandler(_mConnector, OnSourceChanged);
        Activated = false;
    }

    private void OnSourceChanged(object sender, SourceChangedEventArgs args)
    {
        if (args.OldSource is HwndSource oldHwndSource)
        {
            OnSourceDisconnected(oldHwndSource);
        }

        if (args.NewSource is HwndSource newHwndSource)
        {
            OnSourceConnected(newHwndSource);
        }
    }
}