using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.Cache.Memcached
{
	public class NullLogFactory : ILogFactory
	{
		private readonly bool debugEnabled;

		public NullLogFactory(bool debugEnabled = false)
		{
			this.debugEnabled = debugEnabled;
		}

		public ILog GetLogger(Type type)
		{
			return new NullDebugLogger(type)
			{
				IsDebugEnabled = this.debugEnabled
			};
		}

		public ILog GetLogger(string typeName)
		{
			return new NullDebugLogger(typeName)
			{
				IsDebugEnabled = this.debugEnabled
			};
		}
	}
}
