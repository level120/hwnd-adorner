using System.Windows;
using System.Windows.Media;

namespace HwndExtensions.Utils;

/// <summary>
/// RECT
/// </summary>
internal struct RECT
{
    /// <summary>
    /// Left
    /// </summary>
    public int left;

    /// <summary>
    /// Top
    /// </summary>
    public int top;

    /// <summary>
    /// Right
    /// </summary>
    public int right;

    /// <summary>
    /// Bottom
    /// </summary>
    public int bottom;
}

/// <summary>
/// Rect Utility
/// </summary>
internal static class RectUtil
{
    /// <summary>
    /// Double to Int
    /// </summary>
    /// <param name="val">val</param>
    /// <returns>int value</returns>
    public static int DoubleToInt(double val)
    {
        return (0 < val) ? (int)(val + 0.5) : (int)(val - 0.5);
    }

    /// <summary>
    /// Element To Root
    /// </summary>
    /// <param name="rectElement">rectElement</param>
    /// <param name="element">element</param>
    /// <param name="presentationSource">presentationSource</param>
    /// <returns><see cref="Rect"/></returns>
    internal static Rect ElementToRoot(Rect rectElement, Visual element, PresentationSource presentationSource)
    {
        var transformElementToRoot = element.TransformToAncestor(presentationSource.RootVisual);

        return transformElementToRoot.TransformBounds(rectElement);
    }

    /// <summary>
    /// Root To Client
    /// </summary>
    /// <param name="rectRoot">rectRoot</param>
    /// <param name="presentationSource">presentationSource</param>
    /// <returns><see cref="Rect"/></returns>
    internal static Rect RootToClient(Rect rectRoot, PresentationSource presentationSource)
    {
        var target = presentationSource.CompositionTarget;
        var matrixRootTransform = GetVisualTransform(target?.RootVisual);
        var rectRootUntransformed = Rect.Transform(rectRoot, matrixRootTransform);
        var matrixDpi = target?.TransformToDevice ?? Matrix.Identity;

        return Rect.Transform(rectRootUntransformed, matrixDpi);
    }

    /// <summary>
    /// Get Visual Transform
    /// </summary>
    /// <param name="visual">visual</param>
    /// <returns><see cref="Matrix"/></returns>
    internal static Matrix GetVisualTransform(Visual? visual)
    {
        if (visual == null)
        {
            return Matrix.Identity;
        }

        var matrix = Matrix.Identity;
        var offset = VisualTreeHelper.GetOffset(visual);
        var transform = VisualTreeHelper.GetTransform(visual);

        if (transform != null)
        {
            matrix = Matrix.Multiply(matrix, transform.Value);
        }

        matrix.Translate(offset.X, offset.Y);

        return matrix;
    }

    /// <summary>
    /// From Rect
    /// </summary>
    /// <param name="rect">rect</param>
    /// <returns><see cref="RECT"/></returns>
    internal static RECT FromRect(Rect rect)
    {
        return new RECT
        {
            top = DoubleToInt(rect.Y),
            left = DoubleToInt(rect.X),
            bottom = DoubleToInt(rect.Bottom),
            right = DoubleToInt(rect.Right),
        };
    }

    /// <summary>
    /// To Rect
    /// </summary>
    /// <param name="rc">rc</param>
    /// <returns><see cref="Rect"/></returns>
    internal static Rect ToRect(RECT rc)
    {
        return new Rect
        {
            X = rc.left,
            Y = rc.top,
            Width = rc.right - rc.left,
            Height = rc.bottom - rc.top,
        };
    }
}