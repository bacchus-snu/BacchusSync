using log4net;
using log4net.Core;
using log4net.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pGina.Plugin.BacchusSync
{
    internal static class Log
    {
        private static ILog log;

        internal static void Instantiate()
        {
            if (log == null)
                log = LogManager.GetLogger("pGina.Plugin.BacchusSync");
        }
        
        internal static void Debug(object message)
        {
#if DEBUG
            log.Debug(message);
#endif
        }

        internal static void DebugFormat(string format, params object[] args)
        {
#if DEBUG
            log.DebugFormat(format, args);
#endif
        }

        internal static void Warn(object message)
        {
            log.Warn(message);
        }

        internal static void Error(object message)
        {
            log.Error(message);
        }
    }
}
