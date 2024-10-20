using System;
using System.Windows;
using System.Windows.Threading;

namespace HwndExtensions.Utils;

/// <summary>
/// Dispatch UI
/// </summary>
public sealed class DispatchUI
{
    /// <summary>
    /// Main Dispatcher
    /// </summary>
    public static Dispatcher? MainDispatcher;

    /// <summary>
    /// Current Dispatcher
    /// </summary>
    /// <returns><see cref="Dispatcher"/></returns>
    public static Dispatcher? CurrentDispatcher()
    {
        return Application.Current?.Dispatcher ?? MainDispatcher;
    }

    /// <summary>
    /// Verify access to the main UI thread if exists
    /// </summary>
    public static void VerifyAccess()
    {
        var dispatcher = MainDispatcher ?? Application.Current?.Dispatcher;

        dispatcher?.VerifyAccess();
    }

    /// <summary>
    /// Run the current action on the UI thread if exists
    /// <param name="action">The action to run on ui thread</param>
    /// <param name="invokeBlocking">Invoke the action with blocking (invoke) use.</param>
    /// </summary>
    public static void OnUIThread(Action action, bool invokeBlocking = false)
    {
        // if no application is running or the main dispatcher run on the current thread
        if (MainDispatcher == null && Application.Current == null)
        {
            action();
            return;
        }

        // get the current dispatcher, check access and run where needed
        var dispatcherObject = MainDispatcher ?? Application.Current.Dispatcher;

        if (dispatcherObject == null || dispatcherObject.CheckAccess())
        {
            action();

            return;
        }

        // run the invocation blocking or async
        if (invokeBlocking)
        {
            dispatcherObject.Invoke(action);
        }
        else
        {
            dispatcherObject.BeginInvoke(action);
        }
    }

    /// <summary>
    /// On UI thread action on dispatcher async
    /// </summary>
    /// <param name="action">action</param>
    /// <param name="priority">priority</param>
    /// <param name="dispatcher">dispatcher</param>
    /// <returns>result</returns>
    public static bool OnUIThreadAsync(
        Action action, DispatcherPriority priority = DispatcherPriority.Normal, Dispatcher? dispatcher = null)
    {
        dispatcher ??= MainDispatcher ?? Application.Current?.Dispatcher;

        dispatcher?.BeginInvoke(action, priority);

        return dispatcher != null;
    }
}