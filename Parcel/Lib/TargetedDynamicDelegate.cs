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

        public static TargetedDynamicDelegate Bind<T1, T2>(this Action<T1, T2> action)
        {
            return (args) => { action.Invoke((T1)args[0], (T2)args[1]); return null; };
        }

        public static TargetedDynamicDelegate Bind<T1, T2, T3>(this Action<T1, T2, T3> action)
        {
            return (args) => { action.Invoke((T1)args[0], (T2)args[1], (T3)args[2]); return null; };
        }

        public static TargetedDynamicDelegate Bind<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> action)
        {
            return (args) => { action.Invoke((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3]); return null; };
        }

        public static TargetedDynamicDelegate Bind<T1, T2, T3, T4, T5>(this Action<T1, T2, T3, T4, T5> action)
        {
            return (args) => { action.Invoke((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3], (T5)args[4]); return null; };
        }

        public static TargetedDynamicDelegate Bind<TResult>(this Func<TResult> func)
        {
            return (args) => { return (object)func.Invoke(); };
        }

        public static TargetedDynamicDelegate Bind<T1, TResult>(this Func<T1, TResult> func)
        {
            return (args) => { return (object)func.Invoke((T1)args[0]); };
        }

        public static TargetedDynamicDelegate Bind<T1, T2, TResult>(this Func<T1, T2, TResult> func)
        {
            return (args) => { return (object)func.Invoke((T1)args[0], (T2)args[1]); };
        }

        public static TargetedDynamicDelegate Bind<T1, T2, T3, TResult>(this Func<T1, T2, T3, TResult> func)
        {
            return (args) => { return (object)func.Invoke((T1)args[0], (T2)args[1], (T3)args[2]); };
        }

        public static TargetedDynamicDelegate Bind<T1, T2, T3, T4, TResult>(this Func<T1, T2, T3, T4, TResult> func)
        {
            return (args) => { return (object)func.Invoke((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3]); };
        }

        public static TargetedDynamicDelegate Bind<T1, T2, T3, T4, T5, TResult>(this Func<T1, T2, T3, T4, T5, TResult> func)
        {
            return (args) => { return (object)func.Invoke((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3], (T5)args[4]); };
        }

    }
}
