using Libraries.FrameWork;
using System;
using System.Collections.Generic;

namespace Libraries.Cache
{
	public interface ICacheProvider : IProxyBaseObject<ICacheProvider>, IDisposable
	{
		CacheType Type
		{
			get;
		}

		T Get<T>(string key, Func<T> acquire, int cacheTime = 60) where T : class;

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
