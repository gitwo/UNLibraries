using Enyim.Caching;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;
using System;
using System.Collections.Generic;
using System.Net;

namespace Libraries.Cache.Memcached
{
	public class MemcachedClientCache : ICacheClient, IDisposable
	{
		private MemcachedClient _client;

		protected ILog Log
		{
			get
			{
				return LogManager.GetLogger(base.GetType());
			}
		}

		public MemcachedClientCache()
		{
			_client = new MemcachedClient();
		}

		public MemcachedClientCache(IEnumerable<string> hosts)
		{
			List<IPEndPoint> list = new List<IPEndPoint>();
			foreach (string current in hosts)
			{
				string[] array = current.Split(new char[]
				{
					':'
				});
				if (array.Length == 0)
				{
					throw new ArgumentException("'{0}' is not a valid host IP Address: e.g. '127.0.0.0[:11211]'");
				}
				int port = (array.Length == 1) ? 11211 : int.Parse(array[1]);
				IPAddress[] hostAddresses = Dns.GetHostAddresses(array[0]);
				IPAddress[] array2 = hostAddresses;
				for (int i = 0; i < array2.Length; i++)
				{
					IPAddress address = array2[i];
					IPEndPoint item = new IPEndPoint(address, port);
					list.Add(item);
				}
			}
			LoadClient(this.PrepareMemcachedClientConfiguration(list));
		}

		public MemcachedClientCache(IEnumerable<IPEndPoint> ipEndpoints)
		{
			LoadClient(this.PrepareMemcachedClientConfiguration(ipEndpoints));
		}

		public MemcachedClientCache(IMemcachedClientConfiguration memcachedClientConfiguration)
		{
			LoadClient(memcachedClientConfiguration);
		}

		private IMemcachedClientConfiguration PrepareMemcachedClientConfiguration(IEnumerable<IPEndPoint> ipEndpoints)
		{
			MemcachedClientConfiguration memcachedClientConfiguration = new MemcachedClientConfiguration();
			foreach (IPEndPoint current in ipEndpoints)
			{
				memcachedClientConfiguration.Servers.Add(current);
			}
			memcachedClientConfiguration.SocketPool.MinPoolSize = 10;
			memcachedClientConfiguration.SocketPool.MaxPoolSize = 100;
			memcachedClientConfiguration.SocketPool.ConnectionTimeout = new TimeSpan(0, 0, 10);
			memcachedClientConfiguration.SocketPool.DeadTimeout = new TimeSpan(0, 2, 0);
			return memcachedClientConfiguration;
		}

		private void LoadClient(IMemcachedClientConfiguration config)
		{
			LogManager.LogFactory = new EnyimLogFactory();
			_client = new MemcachedClient(config);
		}

		public bool Remove(string key)
		{
			return Execute<bool>(() => _client.Remove(key));
		}

		public object Get(string key)
		{
			return Get<object>(key);
		}

		public T Get<T>(string key)
		{
			return Execute<T>(delegate
			{
				T t = _client.Get<T>(key);
				T result;
				if (t != null)
				{
					result = t;
				}
				else
				{
					result = default(T);
				}
				return result;
			});
		}

		public long Increment(string key, uint amount)
		{
			return Execute<long>(() => (long)_client.Increment(key, 0uL, (ulong)amount));
		}

		public long Decrement(string key, uint amount)
		{
			return Execute<long>(() => (long)_client.Decrement(key, 0uL, (ulong)amount));
		}

		public bool Add<T>(string key, T value)
		{
			return Execute<bool>(() => _client.Store(StoreMode.Add, key, value));
		}

		public bool Set<T>(string key, T value)
		{
			return Execute<bool>(() => _client.Store(StoreMode.Set, key, value));
		}

		public bool Replace<T>(string key, T value)
		{
			return Execute<bool>(() => _client.Store(StoreMode.Replace, key, value));
		}

		public bool Add<T>(string key, T value, DateTime expiresAt)
		{
			return Execute<bool>(() => _client.Store(StoreMode.Add, key, value, expiresAt));
		}

		public bool Set<T>(string key, T value, DateTime expiresAt)
		{
			return Execute<bool>(() => _client.Store(StoreMode.Set, key, value, expiresAt));
		}

		public bool Replace<T>(string key, T value, DateTime expiresAt)
		{
			return Execute<bool>(() => _client.Store(StoreMode.Replace, key, value, expiresAt));
		}

		public bool Add<T>(string key, T value, TimeSpan expiresIn)
		{
			return Execute<bool>(() => _client.Store(StoreMode.Add, key, value, expiresIn));
		}

		public bool Set<T>(string key, T value, TimeSpan expiresIn)
		{
			return Execute<bool>(() => _client.Store(StoreMode.Set, key, value, expiresIn));
		}

		public bool Replace<T>(string key, T value, TimeSpan expiresIn)
		{
			return Execute<bool>(() => _client.Store(StoreMode.Replace, key, value, expiresIn));
		}

		public bool Add(string key, object value)
		{
			return Execute<bool>(() => _client.Store(StoreMode.Add, key, value));
		}

		public bool Set(string key, object value)
		{
			return Execute<bool>(() => _client.Store(StoreMode.Set, key, value));
		}

		public bool Replace(string key, object value)
		{
			return Execute<bool>(() => _client.Store(StoreMode.Replace, key, value));
		}

		public bool Add(string key, object value, DateTime expiresAt)
		{
			return Execute<bool>(() => _client.Store(StoreMode.Add, key, value, expiresAt));
		}

		public bool Set(string key, object value, DateTime expiresAt)
		{
			return Execute<bool>(() => _client.Store(StoreMode.Set, key, value, expiresAt));
		}

		public bool Replace(string key, object value, DateTime expiresAt)
		{
			return Execute<bool>(() => _client.Store(StoreMode.Replace, key, value, expiresAt));
		}

		public bool CheckAndSet(string key, object value, ulong cas)
		{
			return Execute<bool>(() => _client.Cas(StoreMode.Replace, key, value, cas).Result);
		}

		public bool CheckAndSet(string key, object value, ulong cas, DateTime expiresAt)
		{
			return Execute<bool>(() => _client.Cas(StoreMode.Replace, key, value, expiresAt, cas).Result);
		}

		public void FlushAll()
		{
			Execute(delegate
			{
				_client.FlushAll();
			});
		}

		public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
		{
			Dictionary<string, T> dictionary = new Dictionary<string, T>();
			foreach (string current in keys)
			{
				T value = Get<T>(current);
				dictionary[current] = value;
			}
			return dictionary;
		}

		public void SetAll<T>(IDictionary<string, T> values)
		{
			foreach (KeyValuePair<string, T> current in values)
			{
				Set<T>(current.Key, current.Value);
			}
		}

		public IDictionary<string, object> GetAll(IEnumerable<string> keys)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			foreach (string current in keys)
			{
				object value = Get(current);
				dictionary[current] = value;
			}
			return dictionary;
		}

		public void RemoveAll(IEnumerable<string> keys)
		{
			foreach (string current in keys)
			{
				try
				{
					Remove(current);
				}
				catch (Exception exception)
				{
					Log.Error(string.Format("Error trying to remove {0} from memcached", current), exception);
				}
			}
		}

		private T Execute<T>(Func<T> action)
		{
			DateTime now = DateTime.Now;
			Log.DebugFormat("Executing action '{0}'", new object[]
			{
				action.Method.Name
			});
			T result;
			try
			{
				T t = action();
				TimeSpan timeSpan = DateTime.Now - now;
				if (Log.IsDebugEnabled)
				{
					Log.DebugFormat("Action '{0}' executed. Took {1} ms.", new object[]
					{
						action.Method.Name,
						timeSpan.TotalMilliseconds
					});
				}
				result = t;
			}
			catch (Exception ex)
			{
				Log.ErrorFormat("There was an error executing Action '{0}'. Message: {1}", new object[]
				{
					action.Method.Name,
					ex.Message
				});
				throw;
			}
			return result;
		}

		private void Execute(Action action)
		{
			DateTime now = DateTime.Now;
			Log.DebugFormat("Executing action '{0}'", new object[]
			{
				action.Method.Name
			});
			try
			{
				action();
				TimeSpan timeSpan = DateTime.Now - now;
				if (Log.IsDebugEnabled)
				{
					Log.DebugFormat("Action '{0}' executed. Took {1} ms.", new object[]
					{
						action.Method.Name,
						timeSpan.TotalMilliseconds
					});
				}
			}
			catch (Exception ex)
			{
				Log.ErrorFormat("There was an error executing Action '{0}'. Message: {1}", new object[]
				{
					action.Method.Name,
					ex.Message
				});
				throw;
			}
		}

		public void Dispose()
		{
		}

	}
}
