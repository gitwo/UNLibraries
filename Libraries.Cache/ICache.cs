using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.Cache
{
	internal interface ICache : IDisposable
	{
		CacheType Type
		{
			get;
		}

		void Init();

		void SetConfig(CacheConfig config);

		T Get<T>(string key) where T : class;

		void Set<T>(string key, T data, int cacheTime);

		bool IsSet(string key);

		void Remove(string key);

		void RemoveByPattern(string pattern);

		void Clear();

		IList<string> GetAllKey();

		long Increment(string key, uint amount);

		long Decrement(string key, uint amount);

		long GetCountVal(string key);
	}
}
