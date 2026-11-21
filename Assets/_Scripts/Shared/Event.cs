
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;


#region Without Input ============================================================================= Without Input

public interface IEvent
{
    public event Action Always;
    public event Action Once;
}

public class EventCaller : IEvent
{
    public event Action Always { add => always += value; remove => always -= value; }
    public event Action Once { add => once += value; remove => once -= value; }

    protected Action always = null;
    protected Action once = null;

    public void Invoke()
    { always?.Invoke(); once?.Invoke(); once = null; }
}


public interface IAsyncEvent
{
    public event Func<Task> Always;
    public event Func<Task> Once;
}

public class AsyncEventCaller : IAsyncEvent
{
    public event Func<Task> Always { add => always += value; remove => always -= value; }
    public event Func<Task> Once { add => once += value; remove => once -= value; }

    protected Func<Task> always = null;
    protected Func<Task> once = null;

    public async Task InvokeAsync()
    { await AsyncEventHelper.InvokeAsync(always, once); once = null; }

    public async Task ParallelInvokeAsync()
    { await AsyncEventHelper.ParallelInvokeAsync(always, once); once = null; }
}

#endregion Without Input


#region With Input ================================================================================ With Input

public interface IEvent<TIn>
{
    public event Action<TIn> Always;
    public event Action<TIn> Once;
    public event Action AlwaysSimple;
    public event Action OnceSimple;
}

public class EventCaller<TIn> : IEvent<TIn>
{
    public event Action<TIn> Always { add => always += value; remove => always -= value; }
    public event Action<TIn> Once { add => once += value; remove => once -= value; }
    public event Action AlwaysSimple { add => alwaysSimple += value; remove => alwaysSimple -= value; }
    public event Action OnceSimple { add => alwaysSimple += value; remove => alwaysSimple -= value; }

    protected Action<TIn> always = null;
    protected Action<TIn> once = null;
    protected Action alwaysSimple = null;
    protected Action onceSimple = null;

    public void Invoke(TIn input)
    {
        always?.Invoke(input); once?.Invoke(input); once = null;
        alwaysSimple?.Invoke(); onceSimple?.Invoke(); onceSimple = null;
    }
}


public interface IAsyncEvent<TIn>
{
    public event Func<TIn, Task> Always;
    public event Func<TIn, Task> Once;
    public event Func<Task> AlwaysSimple;
    public event Func<Task> OnceSimple;
}

public class AsyncEventCaller<TIn> : IAsyncEvent<TIn>
{
    public event Func<TIn, Task> Always { add => always += value; remove => always -= value; }
    public event Func<TIn, Task> Once { add => once += value; remove => once -= value; }
    public event Func<Task> AlwaysSimple { add => alwaysSimple += value; remove => alwaysSimple -= value; }
    public event Func<Task> OnceSimple { add => onceSimple += value; remove => onceSimple -= value; }

    protected Func<TIn, Task> always = null;
    protected Func<TIn, Task> once = null;
    protected Func<Task> alwaysSimple = null;
    protected Func<Task> onceSimple = null;

    public async Task InvokeAsync(TIn input)
    {
        await Task.WhenAll(AsyncEventHelper.InvokeAsync(input, always, once),
            AsyncEventHelper.InvokeAsync(alwaysSimple, onceSimple)); once = null;
    }

    public async Task ParallelInvokeAsync(TIn input)
    {
        await Task.WhenAll(AsyncEventHelper.ParallelInvokeAsync(input, always, once),
            AsyncEventHelper.ParallelInvokeAsync(alwaysSimple, onceSimple)); once = null;
    }
}

#endregion With Input


#region Async Helper ============================================================================== Async Helper

internal static class AsyncEventHelper
{
    public static async Task InvokeAsync(Func<Task> always, Func<Task> once)
    {
        if (always != null) { foreach (var callback in GetCallbacks(always)) { await callback(); } }
        if (once != null) { foreach (var callback in GetCallbacks(once)) { await callback(); } }
    }

    public static async Task InvokeAsync<TIn>(TIn input, Func<TIn, Task> always, Func<TIn, Task> once)
    {
        if (always != null) { foreach (var callback in GetCallbacks(always)) { await callback(input); } }
        if (once != null) { foreach (var callback in GetCallbacks(once)) { await callback(input); } }
    }


    public static async Task ParallelInvokeAsync(Func<Task> always, Func<Task> once)
    {
        if (always != null) { await Task.WhenAll(GetCallbacks(always).Select(f => f())); }
        if (once != null) { await Task.WhenAll(GetCallbacks(once).Select(f => f())); }
    }
    public static async Task ParallelInvokeAsync<TIn>(TIn input, Func<TIn, Task> always, Func<TIn, Task> once)
    {
        if (always != null) { await Task.WhenAll(GetCallbacks(always).Select(f => f(input))); }
        if (once != null) { await Task.WhenAll(GetCallbacks(once).Select(f => f(input))); }
    }


    private static IEnumerable<Func<Task>> GetCallbacks(Func<Task> func)
    { return func.GetInvocationList().Cast<Func<Task>>(); }

    private static IEnumerable<Func<TIn, Task>> GetCallbacks<TIn>(Func<TIn, Task> func)
    { return func.GetInvocationList().Cast<Func<TIn, Task>>(); }
}

#endregion Async Helper
