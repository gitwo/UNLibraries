using Libraries.FrameWork;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace Libraries.Cache
{

	internal class CacheProvider : ICacheProvider, IProxyBaseObject<ICacheProvider>, IDisposable
	{
		private CacheType _cacheType;

		private static object _lock = new object();

		private ICacheProvider _instance = null;

		private ICache _cacheClient = null;

		private CacheConfig cacheConfig = null;

		public CacheType Type
		{
			get
			{
				return _cacheType;
			}
		}

		public ICacheProvider Instance
		{
			get
			{
				if (_cacheClient == null)
				{
					Init(null);
				}
				return this;
			}
		}

		private ICache CacheClient
		{
			get
			{
				if (_cacheClient == null)
				{
					Init(null);
				}
				return _cacheClient;
			}
		}

		public void Init(BaseConfig config = null)
		{
			lock (_lock)
			{
				if (config != null)
				{
					cacheConfig = (CacheConfig)config;
				}
				else
				{
					LibrariesConfig librariesConfig = ConfigurationManager.GetSection("LibrariesConfig") as LibrariesConfig;
					if (librariesConfig != null)
					{
						cacheConfig = librariesConfig.GetObjByXml<CacheConfig>("CacheConfig");
						if (cacheConfig == null)
						{
							throw new Exception("缺少缓存配置CacheConfig");
						}
					}
				}
				if (cacheConfig == null)
				{
					throw new Exception("缺少缓存配置CacheConfig");
				}
				switch ((CacheType)Enum.Parse(typeof(CacheType), cacheConfig.Provider))
				{
					case CacheType.Memcached:
						_cacheClient = new MemcacheManager();
						break;
					case CacheType.Redis:
						_cacheClient = new RedisManager();
						break;
					default:
						_cacheClient = new MemoryCacheManager();
						break;
				}
				_cacheType = _cacheClient.Type;
				_cacheClient.SetConfig(cacheConfig);
				_cacheClient.Init();
			}
		}

		public T Get<T>(string key, Func<T> acquire, int cacheTime = 60) where T : class
		{
			T result;
			if (CacheClient.IsSet(key))
			{
				result = CacheClient.Get<T>(key);
			}
			else
			{
				T t = acquire();
				CacheClient.Set<T>(key, t, cacheTime);
				result = t;
			}
			return result;
		}

		public T Get<T>(string key) where T : class
		{
			return CacheClient.Get<T>(key);
		}

		public void Set<T>(string key, T data, int cacheTime)
		{
			CacheClient.Set<T>(key, data, cacheTime);
		}

		public bool IsSet(string key)
		{
			return CacheClient.IsSet(key);
		}

		public void Remove(string key)
		{
			CacheClient.Remove(key);
		}

		public void RemoveByPattern(string pattern)
		{
			CacheClient.RemoveByPattern(pattern);
		}

		public void Clear()
		{
			CacheClient.Clear();
		}

		public IList<string> GetAllKey()
		{
			return CacheClient.GetAllKey();
		}

		public void Dispose()
		{
		}

		public long Increment(string key, uint amount)
		{
			return CacheClient.Increment(key, amount);
		}

		public long Decrement(string key, uint amount)
		{
			return CacheClient.Decrement(key, amount);
		}

		public long GetCountVal(string key)
		{
			return CacheClient.GetCountVal(key);
		}
	}
}
