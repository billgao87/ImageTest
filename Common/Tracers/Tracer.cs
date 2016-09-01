using System;

namespace Common.Tracers
{
    public class Tracer
    {
        private readonly NLog.Logger _logger;

        private Tracer(NLog.Logger logger)
        {
            this._logger = logger;
        }

        public Tracer(string name)
            : this(NLog.LogManager.GetLogger(name))
        {
        }

        public Tracer()
        {
            _logger = NLog.LogManager.GetCurrentClassLogger();
        }

        public static Tracer Default { get; private set; }

        static Tracer()
        {
            Default = new Tracer(NLog.LogManager.GetCurrentClassLogger());
        }

        public void Debug(string msg, params object[] args)
        {
            _logger.Debug(msg, args);
        }

        public void Info(string msg, params object[] args)
        {
            _logger.Info(msg, args);
        }

        public void Trace(string msg, params object[] args)
        {
            _logger.Trace(msg, args);
        }

        public void Error(string msg, params object[] args)
        {
            _logger.Error(msg, args);
        }

        public void Exception(Exception e, string msg = null, params object[] args)
        {
            _logger.Error(e, msg, args);
        }

        public void Fatal(string msg, params object[] args)
        {
            _logger.Fatal(msg, args);
        }

        public void LogEnterFunction(string msg)
        {
            string logMsg = "Enter Function: " + msg;
            _logger.Info(logMsg);
        }

        public void LogExitFunction(string msg)
        {
            string logMsg = "Exit Function: " + msg;
            _logger.Info(logMsg);
        }
    }
}
