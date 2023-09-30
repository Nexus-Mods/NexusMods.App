using Avalonia;

namespace NexusMods.App.UI.Extensions;

public static class ReactiveExtensions
{
    /// <summary>
    /// Applies the given class to the given control when a true value is observed
    /// removes the class when a false value is observed.
    /// </summary>
    /// <param name="obs"></param>
    /// <param name="target"></param>
    /// <param name="classToApply"></param>
    /// <typeparam name="TTarget"></typeparam>
    /// <returns></returns>
    public static IDisposable BindToClasses<TTarget>(this IObservable<bool> obs, TTarget target, string classToApply)
        where TTarget : StyledElement
    {
        return obs
            .OnUI()
            .SubscribeWithErrorLogging(logger: default, val =>
            {
                if (val)
                    target.Classes.Add(classToApply);
                else
                    target.Classes.Remove(classToApply);
            });
    }

    /// <summary>
    /// Applies the given class to the given control when a true value is observed, and the false value when false is observed
    /// removes the opposite class when an opposite value is observed.
    /// </summary>
    /// <param name="obs"></param>
    /// <param name="target"></param>
    /// <param name="trueClass"></param>
    /// <param name="falseClass"></param>
    /// <typeparam name="TTarget"></typeparam>
    /// <returns></returns>
    public static IDisposable BindToClasses<TTarget>(this IObservable<bool> obs, TTarget target, string trueClass, string falseClass)
        where TTarget : StyledElement
    {
        return obs
            .OnUI()
            .SubscribeWithErrorLogging(logger: default, val =>
            {
                if (val)
                {
                    target.Classes.Remove(falseClass);
                    target.Classes.Add(trueClass);
                }
                else
                {
                    target.Classes.Remove(trueClass);
                    target.Classes.Add(falseClass);
                }
            });
    }

    /// <summary>
    /// Shorthand for applying "Active" to the given element when true is observed
    /// </summary>
    /// <param name="obs"></param>
    /// <param name="target"></param>
    /// <typeparam name="TTarget"></typeparam>
    /// <returns></returns>
    public static IDisposable BindToActive<TTarget>(this IObservable<bool> obs,
        TTarget target) where TTarget : StyledElement
    {
        return obs.BindToClasses(target, "Active");
    }
}
