using System;

namespace Libraries.Cache.Memcached
{
	public class LogManager
	{
		private static ILogFactory logFactory;

		public static ILogFactory LogFactory
		{
			get
			{
				ILogFactory result;
				if (logFactory == null)
				{
					result = new NullLogFactory(false);
				}
				else
				{
					result = logFactory;
				}
				return result;
			}
			set
			{
				logFactory = value;
			}
		}

		public static ILog GetLogger(Type type)
		{
			return LogFactory.GetLogger(type);
		}

		public static ILog GetLogger(string typeName)
		{
			return LogFactory.GetLogger(typeName);
		}
	}
}
