using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace HwndExtensions.Utils;

/// <summary>
/// WPF Tree Extensions
/// </summary>
public static class WpfTreeExtensions
{
    /// <summary>
    /// Finds a parent of a given item on the visual tree that is of type T.
    /// And the optional predicate returns true for the element
    /// </summary>
    /// <typeparam name="T">The type of the parent</typeparam>
    /// <param name="depObj">The hit test result.</param>
    /// <param name="predicate">predicate</param>
    /// <returns>The matching UIElement, or null if it could not be found.</returns>
    public static T? TryFindVisualAncestor<T>(DependencyObject? depObj, Predicate<T>? predicate = null)
        where T : class
    {
        var current = depObj;

        while (current != null)
        {
            if (current is T match && (predicate == null || predicate(match)))
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return default;
    }

    /// <summary>
    /// Try Find Logical Ancestor
    /// </summary>
    /// <typeparam name="T">The type of the parent</typeparam>
    /// <param name="depObj">The hit test result.</param>
    /// <param name="predicate">predicate</param>
    /// <returns>Founded ancestor</returns>
    public static T? TryFindLogicalAncestor<T>(DependencyObject? depObj, Predicate<T>? predicate = null)
        where T : class
    {
        var current = depObj;

        while (current != null)
        {
            if (current is T match && (predicate == null || predicate(match)))
            {
                return match;
            }

            current = LogicalTreeHelper.GetParent(current);
        }

        return default;
    }

    /// <summary>
    /// Tries to find the top parent - The top-most parent of this type in the visual tree.
    /// </summary>
    /// <typeparam name="T">The type of the parent</typeparam>
    /// <param name="depObj">The hit test result.</param>
    /// <returns>The matching UIElement, or null if it could not be found.</returns>
    public static T? TryFindTopVisualAncestor<T>(DependencyObject depObj)
        where T : class
    {
        var current = default(T);
        var aboveCurrent = TryFindVisualAncestor<T>(depObj);

        while (aboveCurrent != null)
        {
            current = aboveCurrent;
            aboveCurrent = TryFindVisualAncestor<T>(current as DependencyObject);
        }

        return current;
    }

    /// <summary>
    /// Find all visual descendents of type T
    /// </summary>
    /// <typeparam name="T">The type of the parent</typeparam>
    /// <param name="root">root</param>
    /// <param name="searchWithinAFoundT">search within T's descendents for more T's</param>
    public static List<T> FindVisualChildren<T>(DependencyObject? root, bool searchWithinAFoundT = true)
        where T : DependencyObject
    {
        var list = new List<T>();

        if (root is null)
        {
            return list;
        }

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);

            if (child is T dependencyObject)
            {
                list.Add(dependencyObject);

                // this means that an element is not expected to contain elements of his type
                if (!searchWithinAFoundT)
                {
                    continue;
                }
            }

            var childItems = FindVisualChildren<T>(child, searchWithinAFoundT);

            if (childItems.Any())
            {
                list.AddRange(childItems);
            }
        }

        return list;
    }

    /// <summary>
    /// Performs a DFS search for finding the first visual child of type T
    /// </summary>
    /// <typeparam name="T">The type to search for</typeparam>
    /// <param name="root">The root to search under</param>
    /// <returns>The found child</returns>
    public static T? FindFirstChild<T>(DependencyObject? root)
        where T : class
    {
        if (root is null)
        {
            return default;
        }

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);

            if (child is T childT)
            {
                return childT;
            }

            var descendantT = FindFirstChild<T>(child);

            if (descendantT != null)
            {
                return descendantT;
            }
        }

        return default;
    }
}