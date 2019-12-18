using System;

namespace DiceCli
{
    public static class Log
    {
        private static bool _logInitialized;

        /* Log a message object */
        
        public static void Debug(object source, string message)
        {
            Debug(source.GetType(), message);
        }

        public static void Debug(object source, string message, params object[] ps)
        {
            Debug(source.GetType(), string.Format(message, ps));
        }

        public static void Debug(Type source, string message)
        {
            var logger = NLog.LogManager.GetLogger(source.ToString());
            logger.Debug(message);
        }
        
        public static void Info(object source, object message)
        {
            Info(source.GetType(), message);
        }

        public static void Info(Type source, object message)
        {
            var logger = NLog.LogManager.GetLogger(source.ToString());
            logger.Info(message);
        }

        public static void Warn(object source, object message)
        {
            Warn(source.GetType(), message);
        }

        public static void Warn(Type source, object message)
        {
            var logger = NLog.LogManager.GetLogger(source.ToString());
            logger.Warn(message);
        }

        public static void Error(object source, object message)
        {
            Error(source.GetType(), message);
        }

        public static void Error(Type source, object message)
        {
            var logger = NLog.LogManager.GetLogger(source.ToString());
            logger.Error(message);
        }

        public static void Fatal(object source, object message)
        {
            Fatal(source.GetType(), message);
        }

        public static void Fatal(Type source, object message)
        {
            var logger = NLog.LogManager.GetLogger(source.ToString());
            logger.Fatal(message);
        }

        /* Log a message object and exception */
        
        public static void Debug(object source, string message, Exception exception)
        {
            Debug(source.GetType(), message, exception);
        }

        public static void Debug(Type source, string message, Exception exception)
        {
            var logger = NLog.LogManager.GetLogger(source.ToString());
            logger.Debug(exception, message);
        }

        public static void Info(object source, string message, Exception exception)
        {
            Info(source.GetType(), message, exception);
        }

        public static void Info(Type source, string message, Exception exception)
        {
            var logger = NLog.LogManager.GetLogger(source.ToString());
            logger.Info(exception, message);
        }

        public static void Warn(object source, string message, Exception exception)
        {
            Warn(source.GetType(), message, exception);
        }

        public static void Warn(Type source, string message, Exception exception)
        {
            var logger = NLog.LogManager.GetLogger(source.ToString());
            logger.Warn(exception, message);
        }

        public static void Error(object source, string message, Exception exception)
        {
            Error(source.GetType(), message, exception);
        }

        public static void Error(Type source, string message, Exception exception)
        {
            var logger = NLog.LogManager.GetLogger(source.ToString());
            logger.Error(exception, message);
        }

        public static void Fatal(object source, string message, Exception exception)
        {
            Fatal(source.GetType(), message, exception);
        }

        public static void Fatal(Type source, string message, Exception exception)
        {
            var logger = NLog.LogManager.GetLogger(source.ToString());
            logger.Fatal(exception, message);
        }
        
        
        private static void initialize()
        {
            _logInitialized = true;
        }

        public static void EnsureInitialized()
        {
            if (!_logInitialized)
            {
                initialize();
            }
        }

        public static void Shutdown()
        {
            NLog.LogManager.Shutdown();
        }
    }
}