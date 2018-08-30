using System;

namespace pGina.Plugin.BacchusSync
{
    internal static class Log
    {
        internal static void Debug(object message)
        {
            //Console.WriteLine(message);
        }

        internal static void DebugFormat(string format, params object[] args)
        {
            //Console.WriteLine(format, args);
        }

        internal static void Warn(object message)
        {
            Console.WriteLine(message);
        }

        internal static void WarnFormat(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        internal static void Error(object message)
        {
            Console.WriteLine(message);
        }
    }
}
