using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.Cache.Memcached
{
	public class NullDebugLogger : ILog
	{
		public bool IsDebugEnabled
		{
			get;
			set;
		}

		public NullDebugLogger(string type)
		{
		}

		public NullDebugLogger(Type type)
		{
		}

		private static void Log(object message, Exception exception)
		{
		}

		private static void LogFormat(object message, params object[] args)
		{
		}

		private static void Log(object message)
		{
		}

		public void Debug(object message, Exception exception)
		{
		}

		public void Debug(object message)
		{
		}

		public void DebugFormat(string format, params object[] args)
		{
		}

		public void Error(object message, Exception exception)
		{
		}

		public void Error(object message)
		{
		}

		public void ErrorFormat(string format, params object[] args)
		{
		}

		public void Fatal(object message, Exception exception)
		{
		}

		public void Fatal(object message)
		{
		}

		public void FatalFormat(string format, params object[] args)
		{
		}

		public void Info(object message, Exception exception)
		{
		}

		public void Info(object message)
		{
		}

		public void InfoFormat(string format, params object[] args)
		{
		}

		public void Warn(object message, Exception exception)
		{
		}

		public void Warn(object message)
		{
		}

		public void WarnFormat(string format, params object[] args)
		{
		}
	}
}
