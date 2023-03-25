using System;

namespace Parcel.Lib
{
    internal delegate object TargetedDynamicDelegate(object[] args);
    internal static class ActionExtensions
    {

        public static TargetedDynamicDelegate Bind(this Action action)
        {
            return (args) => { action.Invoke(); return null; };
        }

        public static TargetedDynamicDelegate Bind<T1>(this Action<T1> action)
        {
            return (args) => { action.Invoke((T1)args[0]); return null; };
        }

        public static TargetedDynamicDelegate Bind<TResult>(this Func<TResult> func)
        {
            return (args) => { return (object)func.Invoke(); };
        }

    }
}
