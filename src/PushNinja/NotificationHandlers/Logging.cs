using System;
using System.Collections.Generic;

namespace PushNinja
{
    public class LogContent
    {
        public string Type { get; set; }
        public string Message { get; set; }
        public DateTime Time { get; set; }        
    }
    public class NotificationLogger : PushSharp.Core.ILogger
    {

        List<LogContent> _log;        

        public List<LogContent> Log { get { return this._log; } set { this._log = value; } }

        public NotificationLogger()
        {
            _log = new List<LogContent>();
        }       
        public void Debug(string format, params object[] objs)
        {
            _log.Add(new LogContent()
            {
                Time = DateTime.Now,
                Type = "Debug",
                Message = string.Format(format, objs)
            });
        }

        public void Error(string format, params object[] objs)
        {
            _log.Add(new LogContent()
            {
                Time = DateTime.Now,
                Type = "Error",
                Message = string.Format(format, objs)
            });
        }

        public void Info(string format, params object[] objs)
        {
            _log.Add(new LogContent()
            {
                Time = DateTime.Now,
                Type = "Info",
                Message = string.Format(format, objs)
            });
        }

        public void Warning(string format, params object[] objs)
        {
            _log.Add(new LogContent()
            {
                Time = DateTime.Now,
                Type = "Warning",
                Message = string.Format(format, objs)
            });
        }
    }
}