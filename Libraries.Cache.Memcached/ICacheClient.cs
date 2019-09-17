using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.Cache.Memcached
{
	public interface ICacheClient : IDisposable
	{
		bool Remove(string key);

		void RemoveAll(IEnumerable<string> keys);

		T Get<T>(string key);

		long Increment(string key, uint amount);

		long Decrement(string key, uint amount);

		bool Add<T>(string key, T value);

		bool Set<T>(string key, T value);

		bool Replace<T>(string key, T value);

		bool Add<T>(string key, T value, DateTime expiresAt);

		bool Set<T>(string key, T value, DateTime expiresAt);

		bool Replace<T>(string key, T value, DateTime expiresAt);

		bool Add<T>(string key, T value, TimeSpan expiresIn);

		bool Set<T>(string key, T value, TimeSpan expiresIn);

		bool Replace<T>(string key, T value, TimeSpan expiresIn);

		void FlushAll();

		IDictionary<string, T> GetAll<T>(IEnumerable<string> keys);

		void SetAll<T>(IDictionary<string, T> values);
	}
}
