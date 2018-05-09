using System;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Share.Utils
{
    public static class LoggerUtil
    {
        public static void Fatal(this object obj, string fmt, params object[] args)
        {
            var logger = getLogger(obj.GetType().FullName);
            logger?.Fatal(fmt, args);
        }

        public static void Error(this object obj, string fmt, params object[] args)
        {
            var logger = getLogger(obj.GetType().FullName);
            logger?.Error(fmt, args);
        }

        public static void Warn(this object obj, string fmt, params object[] args)
        {
            var logger = getLogger(obj.GetType().FullName);
            logger?.Warn(fmt, args);
        }

        public static void Debug(this object obj, string fmt, params object[] args)
        {
            var logger = getLogger(obj.GetType().FullName);
            logger?.Debug(fmt, args);
        }

        public static void Info(this object obj, string fmt, params object[] args)
        {
            var logger = getLogger(obj.GetType().FullName);
            logger?.Info(fmt, args);
        }

        public static void Trace(this object obj, string fmt, params object[] args)
        {
            var logger = getLogger(obj.GetType().FullName);
            logger?.Trace(fmt, args);
        }

        public static void Log(this object obj, LogLevel level, string fmt, params object[] args)
        {
            var logger = getLogger(obj.GetType().FullName);
            logger?.Log(level, fmt, args);
        }

        public static void Fatal(Type type, string fmt, params object[] args)
        {
            var logger = getLogger(type.FullName);
            logger?.Fatal(fmt, args);
        }

        public static void Error(Type type, string fmt, params object[] args)
        {
            var logger = getLogger(type.FullName);
            logger?.Error(fmt, args);
        }

        public static void Warn(Type type, string fmt, params object[] args)
        {
            var logger = getLogger(type.FullName);
            logger?.Warn(fmt, args);
        }

        public static void Debug(Type type, string fmt, params object[] args)
        {
            var logger = getLogger(type.FullName);
            logger?.Debug(fmt, args);
        }

        public static void Info(Type type, string fmt, params object[] args)
        {
            var logger = getLogger(type.FullName);
            logger?.Info(fmt, args);
        }

        public static void Trace(Type type, string fmt, params object[] args)
        {
            var logger = getLogger(type.FullName);
            logger?.Trace(fmt, args);
        }

        public static void Log(Type type, LogLevel level, string fmt, params object[] args)
        {
            var logger = getLogger(type.FullName);
            logger?.Log(level, fmt, args);
        }

        private static bool inited = false;

        private static NLog.Logger getLogger(string name)
        {
            if (!inited)
            {
                var cfg = new LoggingConfiguration();

                var consoleTarget = new ColoredConsoleTarget();
                cfg.AddTarget("console", consoleTarget);

                var fileTarget = new FileTarget();
                cfg.AddTarget("file", fileTarget);

                consoleTarget.Layout = @"${date:format=HH\:mm\:ss} [${logger}] ${message}";
                fileTarget.FileName = "${basedir}/file.txt";
                fileTarget.Layout = @"${date:format=HH\:mm\:ss} [${logger}] ${message}";

                var rule1 = new LoggingRule("*", NLog.LogLevel.Trace, consoleTarget);
                cfg.LoggingRules.Add(rule1);

                var rule2 = new LoggingRule("*", NLog.LogLevel.Trace, fileTarget);
                cfg.LoggingRules.Add(rule2);

                LogManager.Configuration = cfg;

                inited = true;
            }
            return LogManager.GetLogger(name);
        }
    }
}
