using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.Cache.Memcached
{
	public interface ILogFactory
	{
		ILog GetLogger(Type type);

		ILog GetLogger(string typeName);
	}
}
