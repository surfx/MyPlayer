using Avalonia.Threading;

namespace MyPlayer.classes.util.threads;

public static class InvokeAux
{
    public static TResult GetValue<TControl, TResult>(TControl ctrl, Func<TControl, TResult> getter)
    {
        if (Dispatcher.UIThread.CheckAccess())
            return getter(ctrl);
        return Dispatcher.UIThread.InvokeAsync(() => getter(ctrl)).GetAwaiter().GetResult();
    }

    public static void Access<TControl>(TControl ctrl, Action<TControl> setter)
    {
        if (Dispatcher.UIThread.CheckAccess())
            setter(ctrl);
        else
            Dispatcher.UIThread.Post(() => setter(ctrl));
    }

    public static async Task AccessAsync<TControl>(TControl ctrl, Action<TControl> setter)
    {
        if (Dispatcher.UIThread.CheckAccess())
            setter(ctrl);
        else
            await Dispatcher.UIThread.InvokeAsync(() => setter(ctrl));
    }
}
