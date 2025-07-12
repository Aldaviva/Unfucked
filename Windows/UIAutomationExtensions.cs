using ManagedWinapi.Windows;
using System.Diagnostics.Contracts;
using System.Windows.Automation;
using ThrottleDebounce.Retry;

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

    private static readonly Exception ElementNotFound = new ApplicationException("element not found");

    private static readonly RetryOptions WaitForFirstOptions = new() {
        Delay          = Delays.Power(TimeSpan.FromMilliseconds(8), max: TimeSpan.FromMilliseconds(500)),
        IsRetryAllowed = (exception, _) => exception is not ArgumentException
    };

    private static RetryOptions GetWaitForFirstOptions(TimeSpan maxWait, CancellationToken cancellationToken) => WaitForFirstOptions with {
        MaxOverallDuration = maxWait > TimeSpan.Zero ? maxWait : null,
        CancellationToken = cancellationToken
    };

    /// <summary>
    /// Find the first matching child or descendant element of this UI Automation element, waiting for it to appear if it's not immediately available.
    /// </summary>
    /// <param name="parent">Parent or ancestor element on which to base the search.</param>
    /// <param name="scope">Whether to find children (non-recursive) or descendants (recursive) of <paramref name="parent"/>. Other values of <see cref="TreeScope"/>, such as <see cref="TreeScope.Parent"/>, are not allowed.</param>
    /// <param name="condition">Condition, such as a <see cref="PropertyCondition"/>, used to find matching elements.</param>
    /// <param name="maxWait">How long to wait for a matching element to appear if one is not immediately available. If not specified, it will wait forever, or until a match is found or <paramref name="cancellationToken"/> is canceled.</param>
    /// <param name="cancellationToken">Used to cancel a wait before it finishes.</param>
    /// <returns>The first child or descendant element of <paramref name="parent"/> that matches <paramref name="condition"/>, or <c>null</c> if either <paramref name="maxWait"/> elapsed or <paramref name="cancellationToken"/> was canceled before a match could be found.</returns>
    /// <exception cref="ArgumentException"><paramref name="scope"/> is neither <see cref="TreeScope.Children"/> nor <see cref="TreeScope.Descendants"/>.</exception>
    /// <exception cref="TaskCanceledException"><paramref name="cancellationToken"/> was canceled</exception>
    // ExceptionAdjustment: M:ThrottleDebounce.Retry.Retrier.Attempt``1(System.Func{System.Int64,``0},ThrottleDebounce.Retry.RetryOptions) -T:System.Exception
    [Pure]
    public static AutomationElement? WaitForFirst(this AutomationElement parent, TreeScope scope, Condition condition, TimeSpan maxWait = default, CancellationToken cancellationToken = default) {
        try {
            return Retrier.Attempt(_ => parent.FindFirst(scope, condition) ?? throw ElementNotFound, GetWaitForFirstOptions(maxWait, cancellationToken));
        } catch (Exception e) when (e is not OutOfMemoryException) {
            return null;
        }
    }

#pragma warning disable CS1573 // param validation is unaware of inheritdoc
    /// <inheritdoc cref="WaitForFirst" />
    /// <param name="resultTransformer">After finding the first matching child element, apply this function to it to produce the method's final return value, instead of just returning the matched element directly. If this function throws an exception, this method will retry, so it is safe to try to access an element that may not exist yet, because it will just wait until it's available.</param>
    // ExceptionAdjustment: M:ThrottleDebounce.Retry.Retrier.Attempt``1(System.Func{System.Int64,``0},ThrottleDebounce.Retry.RetryOptions) -T:System.Exception
    [Pure]
    public static TResult? WaitForFirst<TResult>(this AutomationElement parent, TreeScope scope, Condition condition, Func<AutomationElement, TResult> resultTransformer, TimeSpan maxWait = default,
                                                 CancellationToken cancellationToken = default) where TResult: class {
        try {
            return Retrier.Attempt(_ => parent.FindFirst(scope, condition) is { } el ? resultTransformer(el) : throw ElementNotFound, GetWaitForFirstOptions(maxWait, cancellationToken));
        } catch (Exception e) when (e is not OutOfMemoryException) {
            return null;
        }
    }

    /// <inheritdoc cref="WaitForFirst" />
    // ExceptionAdjustment: M:ThrottleDebounce.Retry.Retrier.Attempt``1(System.Func{System.Int64,``0},ThrottleDebounce.Retry.RetryOptions) -T:System.Exception
    [Pure]
    public static async Task<AutomationElement?> WaitForFirstAsync(this AutomationElement parent, TreeScope scope, Condition condition, TimeSpan maxWait = default,
                                                                   CancellationToken cancellationToken = default) {
        try {
            return await Retrier.Attempt(_ => parent.FindFirst(scope, condition) is { } el ? Task.FromResult(el) : Task.FromException<AutomationElement>(ElementNotFound),
                GetWaitForFirstOptions(maxWait, cancellationToken)).ConfigureAwait(false);
        } catch (Exception e) when (e is not OutOfMemoryException) {
            return null;
        }
    }

    /// <inheritdoc cref="WaitForFirst{T}" />
    // ExceptionAdjustment: M:ThrottleDebounce.Retry.Retrier.Attempt``1(System.Func{System.Int64,``0},ThrottleDebounce.Retry.RetryOptions) -T:System.Exception
    [Pure]
    public static async Task<TResult?> WaitForFirstAsync<TResult>(this AutomationElement parent, TreeScope scope, Condition condition, Func<AutomationElement, Task<TResult>> resultTransformer,
                                                                  TimeSpan maxWait = default, CancellationToken cancellationToken = default) where TResult: class {
        try {
            return await Retrier.Attempt(async _ => parent.FindFirst(scope, condition) is { } el ? await resultTransformer(el).ConfigureAwait(false) : throw ElementNotFound,
                GetWaitForFirstOptions(maxWait, cancellationToken)).ConfigureAwait(false);
        } catch (Exception e) when (e is not OutOfMemoryException) {
            return null;
        }
    }
#pragma warning restore CS1573

}