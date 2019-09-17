using Libraries.FrameWork;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;

namespace Libraries.Cache
{
	internal class RedisManager : ICache, IDisposable
	{
		private static string serverUrl;

		private CacheConfig cacheConfig = null;

		private static IConnectionMultiplexer cacheClient;

		private static IDatabase db
		{
			get
			{
				return cacheClient.GetDatabase(-1, null);
			}
		}

		public CacheType Type
		{
			get
			{
				return CacheType.Redis;
			}
		}

		public void SetConfig(CacheConfig config)
		{
			cacheConfig = config;
		}

		public void Init()
		{
			if (cacheConfig == null)
			{
				try
				{
					LibrariesConfig librariesConfig = ConfigurationManager.GetSection("LibrariesConfig") as LibrariesConfig;
					if (librariesConfig != null)
					{
						cacheConfig = librariesConfig.GetObjByXml<CacheConfig>("CacheConfig");
						if (cacheConfig == null)
						{
							throw new Exception("缺少本地缓存配置节点");
						}
						serverUrl = cacheConfig.Url;
					}
				}
				catch (Exception var_1_6C)
				{
					throw new Exception("缺少Redis配置节点");
				}
			}
			if (cacheClient == null)
			{
				lock (typeof(RedisManager))
				{
					cacheClient = ConnectionMultiplexer.Connect(cacheConfig.Url, null);
				}
			}
		}

		public T Get<T>(string key) where T : class
		{
			key = key.ToLower();
			RedisValue redisValue = db.StringGet(key, CommandFlags.None);
			T result;
			if (typeof(T).Equals(typeof(string)))
			{
				result = (redisValue.ToString() as T);
			}
			else
			{
				if (redisValue.ToString() == null)
				{
					return null;
				}
				result = redisValue.ToString().ToObject<T>();
			}
			return result;
		}

		public void Set<T>(string key, T data, int cacheTime)
		{
			key = key.ToLower();
			if (data != null)
			{
				if (typeof(T).Equals(typeof(string)))
				{
					db.StringSet(key, data.ToString(), new TimeSpan?(TimeSpan.FromMinutes((double)cacheTime)), When.Always, CommandFlags.None);
				}
				else
				{
					db.StringSet(key, data.ToJson(), new TimeSpan?(TimeSpan.FromMinutes((double)cacheTime)), When.Always, CommandFlags.None);
				}
			}
		}

		public bool IsSet(string key)
		{
			key = key.ToLower();
			return db.KeyExists(key, CommandFlags.None);
		}

		public void Remove(string key)
		{
			key = key.ToLower();
			db.KeyDelete(key, CommandFlags.None);
		}

		public void RemoveByPattern(string pattern)
		{
			pattern = string.Format("*{0}*", pattern.ToLower());
			EndPoint[] endPoints = cacheClient.GetEndPoints(true);
			EndPoint[] array = endPoints;
			for (int i = 0; i < array.Length; i++)
			{
				EndPoint endpoint = array[i];
				IServer server = cacheClient.GetServer(endpoint, null);
				ConfigurationOptions configurationOptions = ConfigurationOptions.Parse(server.Multiplexer.Configuration);
				IEnumerable<RedisKey> source = server.Keys(configurationOptions.DefaultDatabase ?? 0, pattern, 2147483647, 0L, 0, CommandFlags.None);
				long num = db.KeyDelete(source.ToArray<RedisKey>(), CommandFlags.None);
			}
		}

		public void Clear()
		{
			EndPoint endpoint = db.IdentifyEndpoint(default(RedisKey), CommandFlags.None);
			IServer server = cacheClient.GetServer(endpoint, null);
			server.FlushDatabase(db.Database, CommandFlags.None);
		}

		public IList<string> GetAllKey()
		{
			IList<string> list = new List<string>();
			IEnumerable<RedisKey> enumerable = null;
			EndPoint[] endPoints = cacheClient.GetEndPoints(true);
			EndPoint[] array = endPoints;
			for (int i = 0; i < array.Length; i++)
			{
				EndPoint endpoint = array[i];
				IServer server = cacheClient.GetServer(endpoint, null);
				ConfigurationOptions configurationOptions = ConfigurationOptions.Parse(server.Multiplexer.Configuration);
				enumerable = server.Keys(configurationOptions.DefaultDatabase ?? 0, "*", 2147483647, 0L, 0, CommandFlags.None);
			}
			if (enumerable != null)
			{
				foreach (RedisKey current in enumerable)
				{
					list.Add(current.ToString());
				}
			}
			return list;
		}

		public void Dispose()
		{
			cacheClient.Dispose();
		}

		public long Increment(string key, uint amount)
		{
			return db.HashIncrement(key, "count", (long)((ulong)amount), CommandFlags.None);
		}

		public long Decrement(string key, uint amount)
		{
			return db.HashDecrement(key, "count", (long)((ulong)amount), CommandFlags.None);
		}

		public long GetCountVal(string key)
		{
			RedisValue value = db.HashGet(key, "count", CommandFlags.None);
			long result;
			if (value.IsNullOrEmpty)
			{
				result = 0L;
			}
			else
			{
				result = long.Parse(value);
			}
			return result;
		}
	}

	public static class JsonHelper
	{
		public static string ToJson(this object source)
		{
			return JsonConvert.SerializeObject(source);
		}

		public static T ToObject<T>(this string jsonString) where T : class
		{
			return JsonConvert.DeserializeObject<T>(jsonString);
		}
	}
}
