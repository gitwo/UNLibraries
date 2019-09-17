using Libraries.Cache.Memcached;
using Libraries.FrameWork;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Enyim.Caching;

namespace Libraries.Cache
{
	internal class MemcacheManager : ICache, IDisposable
	{
		private static ICacheClient cacheClient;

		private CacheConfig cacheConfig = null;

		public CacheType Type
		{
			get
			{
				return CacheType.Memcached;
			}
		}

		public void SetConfig(CacheConfig config)
		{
			this.cacheConfig = config;
		}

		public void Init()
		{
			if (this.cacheConfig == null)
			{
				LibrariesConfig librariesConfig = ConfigurationManager.GetSection("LibrariesConfig") as LibrariesConfig;
				if (librariesConfig != null)
				{
					this.cacheConfig = librariesConfig.GetObjByXml<CacheConfig>("CacheConfig");
					if (this.cacheConfig == null)
					{
						throw new Exception("缺少本地缓存配置节点");
					}
				}
			}
			if (cacheClient == null)
			{
				lock (typeof(MemcacheManager))
				{
					cacheClient = new MemcachedClientCache(cacheConfig.Url.Split(new char[]
					{
						','
					}).ToList<string>());
				}
			}
		}

		public T Get<T>(string key) where T : class
		{
			return cacheClient.Get<T>(key);
		}

		public void Set<T>(string key, T data, int cacheTime)
		{
			cacheClient.Set<T>(key, data, new TimeSpan(0, cacheTime, 0));
		}

		public bool IsSet(string key)
		{
			return cacheClient.Get<object>(key) != null;
		}

		public void Remove(string key)
		{
			cacheClient.Remove(key);
		}

		public void RemoveByPattern(string pattern)
		{
			throw new NotImplementedException();
		}

		public void Clear()
		{
			cacheClient.FlushAll();
		}

		public IList<string> GetAllKey()
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			cacheClient.Dispose();
		}

		public long Increment(string key, uint amount)
		{
			return cacheClient.Increment(key, amount);
		}

		public long Decrement(string key, uint amount)
		{
			return cacheClient.Decrement(key, amount);
		}

		public long GetCountVal(string key)
		{
			return cacheClient.Increment(key, 0u);
		}
	}
}
