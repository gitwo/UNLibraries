using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text.RegularExpressions;

namespace Libraries.Cache
{
	internal class MemoryCacheManager : ICache, IDisposable
	{
		private CacheConfig cacheConfig = null;

		public CacheType Type
		{
			get
			{
				return CacheType.Local;
			}
		}

		protected ObjectCache Cache
		{
			get
			{
				return MemoryCache.Default;
			}
		}

		public void Init()
		{
		}

		public void SetConfig(CacheConfig config)
		{
			cacheConfig = config;
		}

		public T Get<T>(string key) where T : class
		{
			return (T)((object)Cache[key]);
		}

		public void Set<T>(string key, T data, int cacheTime)
		{
			if (data != null)
			{
				CacheItemPolicy cacheItemPolicy = new CacheItemPolicy();
				cacheItemPolicy.AbsoluteExpiration = DateTime.Now + TimeSpan.FromMinutes((double)cacheTime);
				Cache.Add(new CacheItem(key, data), cacheItemPolicy);
			}
		}

		public bool IsSet(string key)
		{
			return Cache.Contains(key, null);
		}

		public void Remove(string key)
		{
			Cache.Remove(key, null);
		}

		public void RemoveByPattern(string pattern)
		{
			Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, object> current in ((IEnumerable<KeyValuePair<string, object>>)Cache))
			{
				if (regex.IsMatch(current.Key))
				{
					list.Add(current.Key);
				}
			}
			foreach (string current2 in list)
			{
				Remove(current2);
			}
		}

		public void Clear()
		{
			foreach (KeyValuePair<string, object> current in ((IEnumerable<KeyValuePair<string, object>>)Cache))
			{
				Remove(current.Key);
			}
		}

		public IList<string> GetAllKey()
		{
			return (from item in Cache
					select item.Key).ToList<string>();
		}

		public void Dispose()
		{
		}

		public long Increment(string key, uint amount)
		{
			throw new Exception("本地缓存没有Increment功能");
		}

		public long Decrement(string key, uint amount)
		{
			throw new Exception("本地缓存没有Decrement功能");
		}

		public long GetCountVal(string key)
		{
			throw new Exception("本地缓存没有GetCount功能");
		}
	}
}
