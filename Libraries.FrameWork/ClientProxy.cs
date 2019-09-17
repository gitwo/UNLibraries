using System;
using System.Collections.Generic;
using System.Reflection;

namespace Libraries.FrameWork
{
	public class ClientProxy
	{
		private static readonly Dictionary<string, object> factories = new Dictionary<string, object>();

		private static readonly object sync = new object();

		public static T GetInstance<T>(BaseConfig config = null) where T : IProxyBaseObject<T>
		{
			T result;
			lock (sync)
			{
				object obj2 = null;
				string key = typeof(T).FullName + ((config != null) ? ("." + config.Provider) : "");
				if (config != null || !factories.TryGetValue(key, out obj2))
				{
					string @namespace = typeof(T).Namespace;
					if (@namespace != null)
					{
						Type[] types = Assembly.Load(@namespace).GetTypes();
						for (int i = 0; i < types.Length; i++)
						{
							Type type = types[i];
							if (type.IsClass && !type.IsAbstract && type.GetInterface(typeof(T).Name) != null)
							{
								IProxyBaseObject<T> proxyBaseObject = Activator.CreateInstance(type) as IProxyBaseObject<T>;
								if (proxyBaseObject != null)
								{
									proxyBaseObject.Init(config);
									obj2 = proxyBaseObject.Instance;
								}
								break;
							}
						}
					}
					if (obj2 == null)
					{
						throw new Exception("无法实例化该接口");
					}
					factories.Remove(key);
					factories.Add(key, obj2);
				}
				result = (T)obj2;
			}
			return result;
		}
	}
}
