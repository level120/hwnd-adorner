using System.Windows;

namespace HwndExtensions.Host;

/// <summary>
/// Hwnd Holder Interface
/// </summary>
public interface IHwndHolder
{
    /// <summary>
    /// Latest Hwnd Bounds
    /// </summary>
    Rect LatestHwndBounds { get; }

    /// <summary>
    /// Freezed Hwnd Bounds
    /// </summary>
    Rect FreezedHwndBounds { get; }

    /// <summary>
    /// Collapse Hwnd
    /// </summary>
    /// <param name="freezeBounds">freezeBounds</param>
    void CollapseHwnd(bool freezeBounds = false);

    /// <summary>
    /// Freeze Hwnd Bounds
    /// </summary>
    void FreezeHwndBounds();

    /// <summary>
    /// Expand Hwnd
    /// </summary>
    void ExpandHwnd();
}