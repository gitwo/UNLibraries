using Libraries.FrameWork;
using Libraries.FrameWork.Model;
using Libraries.NoSqlDB.Model;
using System;
using System.Collections.Generic;

namespace Libraries.NoSqlDB
{
	internal class NoSqlDBProvider:INoSqlDBProvider,IProxyBaseObject<INoSqlDBProvider>,IDisposable
	{

		private INoSqlDBProvider _instance = null;

		private NoSqlDBConfig _noSqlDbConfig = null;

		private static INoSqlDBClient _logClient;

		private static object _lock = new object();

		private INoSqlDBClient LogManager
		{
			get
			{
				if (_logClient == null)
				{
					Init(null);
				}
				return _logClient;
			}
		}

		public INoSqlDBProvider Instance
		{
			get
			{
				return _instance;
			}
		}

		public void Init(BaseConfig config = null)
		{
			lock (_lock)
			{
				if (_logClient == null)
				{
					if (config != null)
					{
						_noSqlDbConfig = (NoSqlDBConfig)config;
					}
					else
					{
						_noSqlDbConfig = ConfigUtil.GetConfig<NoSqlDBConfig>("");
					}
					if (_noSqlDbConfig != null)
					{
						string text = _noSqlDbConfig.Provider.ToLower();
						if (text != null)
						{
							if (text == "mongodb")
							{
								INoSqlDBClient noSqlDBClient = new MongoDBClient();
								_logClient = noSqlDBClient.Connect(_noSqlDbConfig);
								goto IL_C8;
							}
						}
						throw new Exception(string.Format("没有可提供的{0}调用", _noSqlDbConfig.Provider));
					}
					throw new Exception("缺少本地MongoDBConfig配置节点");
				}
			IL_C8:;
			}
		}

		public void Dispose()
		{
			LogManager.Dispose();
		}

		public NoSqlDBProvider()
		{
			_instance = this;
		}

		public IPageList<T> Query<T>(FilterCondition filter) where T : IBaseObject
		{
			return LogManager.Query<T>(filter);
		}

		public void Insert<T>(T model) where T : IBaseObject
		{
			LogManager.Insert<T>(model);
		}

		public void InsertSync<T>(T model) where T : IBaseObject
		{
			LogManager.InsertSync<T>(model);
		}

		public void BatchInsert<T>(IList<T> model) where T : IBaseObject
		{
			LogManager.BatchInsert<T>(model);
		}

		public void BatchInsertSync<T>(IList<T> model) where T : IBaseObject
		{
			LogManager.BatchInsertSync<T>(model);
		}

		public void Delete<T>(string key, object value) where T : IBaseObject
		{
			LogManager.Delete<T>(key, value);
		}

		public void DeleteSync<T>(string key, object value) where T : IBaseObject
		{
			LogManager.DeleteSync<T>(key, value);
		}

		public void DropTable<T>() where T : IBaseObject
		{
			LogManager.DropTable<T>();
		}

		public void CreateIndex<T>(IndexField index) where T : IBaseObject
		{
			LogManager.CreateIndex<T>(index);
		}

		public void CreateIndexAsync<T>(IndexField index) where T : IBaseObject
		{
			LogManager.CreateIndexAsync<T>(index);
		}

		public void CreateIndexs<T>(IEnumerable<IndexField> indexs) where T : IBaseObject
		{
			LogManager.CreateIndexs<T>(indexs);
		}

		public void CreateIndexsAsync<T>(IEnumerable<IndexField> indexs) where T : IBaseObject
		{
			LogManager.CreateIndexsAsync<T>(indexs);
		}

		public void DropAllIndex<T>() where T : IBaseObject
		{
			LogManager.DropAllIndex<T>();
		}

		public void DropAllIndexAsync<T>() where T : IBaseObject
		{
			LogManager.DropAllIndexAsync<T>();
		}

		public IEnumerable<IndexField> GetIndexs<T>() where T : IBaseObject
		{
			return LogManager.GetIndexs<T>();
		}
	}
}
