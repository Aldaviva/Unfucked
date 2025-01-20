using ManagedWinapi.Windows;
using System.Diagnostics.Contracts;
using System.Windows.Automation;

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with Windows UI Automation and Win32 windows.
/// </summary>
public static class UIAutomationExtensions {

    /// <summary>
    /// Convert a UI Automation element to its native Win32 window handle.
    /// </summary>
    /// <param name="element">UI Automation element.</param>
    /// <returns>Native Win32 window handle that refers to the same window as <paramref name="element"/>, or <c>null</c> if the UI element for <paramref name="element"/> no longer exists (race condition lost).</returns>
    [Pure]
    public static IntPtr? ToHwnd(this AutomationElement element) {
        try {
            return new IntPtr(element.Current.NativeWindowHandle);
        } catch (ElementNotAvailableException) {
            return null;
        }
    }

    /// <summary>
    /// Convert a UI Automation element to an mwinapi (Managed Windows API, <see href="https://mwinapi.sourceforge.net"/>) <see cref="SystemWindow"/>.
    /// </summary>
    /// <param name="element">UI Automation element.</param>
    /// <returns>mwinapi <see cref="SystemWindow"/> that wraps the same window as <paramref name="element"/>, or <c>null</c> if the window for <paramref name="element"/> was destroyed in parallel.</returns>
    [Pure]
    public static SystemWindow? ToSystemWindow(this AutomationElement element) {
        return element.ToHwnd() is { } hwnd ? new SystemWindow(hwnd) : null;
    }

    /// <summary>
    /// Convert an mwinapi (Managed Windows API, <see href="https://mwinapi.sourceforge.net"/>) <see cref="SystemWindow"/> to a UI Automation element.
    /// </summary>
    /// <param name="window">mwinapi <see cref="SystemWindow"/></param>
    /// <returns>UI Automation element that wraps the same window as <paramref name="window"/>, or <c>null</c> if the <paramref name="window"/> was destroyed in parallel.</returns>
    [Pure]
    public static AutomationElement? ToAutomationElement(this SystemWindow window) {
        return window.HWnd == IntPtr.Zero ? null : AutomationElement.FromHandle(window.HWnd);
    }

    /// <summary>
    /// List all children of an UI Automation element (1 level, non-recursive, no grandchildren).
    /// </summary>
    /// <param name="parent">The element whose children you want to list.</param>
    /// <returns>Collection of UI Automation elements whose parent element is <paramref name="parent"/>.</returns>
    [Pure]
    public static IEnumerable<AutomationElement> Children(this AutomationElement parent) {
        return parent.FindAll(TreeScope.Children, Condition.TrueCondition).Cast<AutomationElement>();
    }

    /// <summary>
    /// <para>Create an <see cref="AndCondition"/> or <see cref="OrCondition"/> for a <paramref name="property"/> from a series of <paramref name="values"/>, which have fewer than 2 items in it.</para>
    /// <para>This avoids a crash in the <see cref="AndCondition"/> and <see cref="OrCondition"/> constructors if the array has size 1.</para>
    /// </summary>
    /// <param name="property">The name of the UI property to match against, such as <see cref="AutomationElement.NameProperty"/> or <see cref="AutomationElement.AutomationIdProperty"/>.</param>
    /// <param name="and"><c>true</c> to make a conjunction (AND), <c>false</c> to make a disjunction (OR)</param>
    /// <param name="values">Zero or more property values to match against.</param>
    /// <returns>A <see cref="Condition"/> that matches the values against the property, without throwing an <see cref="ArgumentException"/> if <paramref name="values"/> has length &lt; 2.</returns>
    [Pure]
    public static Condition SingletonSafePropertyCondition(AutomationProperty property, bool and, IEnumerable<string> values) {
        Condition[] propertyConditions = values.Select<string, Condition>(allowedValue => new PropertyCondition(property, allowedValue)).ToArray();
        return propertyConditions.Length switch {
            0 => and ? Condition.TrueCondition : Condition.FalseCondition,
            1 => propertyConditions[0],
            _ => and ? new AndCondition(propertyConditions) : new OrCondition(propertyConditions)
        };
    }

}