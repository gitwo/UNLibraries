using System;

namespace Libraries.Cache.Memcached
{
	public class EnyimLogFactory : ILogFactory
	{
		public ILog GetLogger(Type type)
		{
			return new EnyimLogger(LogManager.GetLogger(type));
		}

		public ILog GetLogger(string typeName)
		{
			return new EnyimLogger(LogManager.GetLogger(typeName));
		}
	}
}
