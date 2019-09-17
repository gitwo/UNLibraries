using Libraries.FrameWork.Model;
using Libraries.NoSqlDB.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Libraries.NoSqlDB
{
	internal class MongoDBClient : INoSqlDBClient, IDisposable
	{
		private static IDictionary<string, IDictionary<string, IDictionary<string, PropertyInfo>>> fields = new Dictionary<string, IDictionary<string, IDictionary<string, PropertyInfo>>>();

		private static object _lock = new object();

		private IMongoDatabase client = null;

		private NoSqlDBConfig config = null;

		public IMongoDatabase Client
		{
			get
			{
				return this.client;
			}
		}

		public NoSqlDBConfig Config
		{
			get
			{
				return this.config;
			}
		}

		public INoSqlDBClient Connect(NoSqlDBConfig config)
		{
			this.config = config;
			MongoClient mongoClient = new MongoClient(config.ConnectionString);
			this.client = mongoClient.GetDatabase(config.DBName, null);
			return this;
		}

		public IPageList<T> Query<T>(FilterCondition filter) where T : IBaseObject
		{
			this.CheckType<T>();
			IMongoCollection<T> collection = this.GetCollection<T>();
			FilterDefinitionBuilder<T> filter2 = Builders<T>.Filter;
			SortDefinitionBuilder<T> sort = Builders<T>.Sort;
			FilterDefinition<T> filterDefinition = filter2.Empty;
			foreach (FilterValue current in filter.Fields)
			{
				switch (current.FilterType)
				{
					case FilterType.LTE:
						filterDefinition &= filter2.Lte<object>(current.FieldName, current.Value);
						break;
					case FilterType.GTE:
						filterDefinition &= filter2.Gte<object>(current.FieldName, current.Value);
						break;
					case FilterType.LT:
						filterDefinition &= filter2.Lt<object>(current.FieldName, current.Value);
						break;
					case FilterType.GT:
						filterDefinition &= filter2.Gt<object>(current.FieldName, current.Value);
						break;
					case FilterType.EQ:
						goto IL_12E;
					case FilterType.Like:
						filterDefinition &= filter2.Regex(current.FieldName, new Regex(current.Value.ToString(), RegexOptions.IgnoreCase));
						break;
					default:
						goto IL_12E;
				}
				continue;
			IL_12E:
				filterDefinition &= filter2.Eq<object>(current.FieldName, current.Value);
			}
			Task<long> task = collection.CountAsync(filterDefinition, null, default(CancellationToken));
			SortDefinition<T> sort2 = (filter.Direction == OrderDirection.DESC) ? sort.Descending(filter.OrderBy) : sort.Ascending(filter.OrderBy);
			Task<List<T>> task2 = collection.Find(filterDefinition, null).Sort(sort2).Limit(new int?(filter.PageSize)).Skip(new int?((filter.PageIndex - 1) * filter.PageSize)).ToListAsync(default(CancellationToken));
			return new PageList<T>(task2.Result, filter.PageIndex, filter.PageSize, (int)task.Result);
		}

		public void Insert<T>(T model) where T : IBaseObject
		{
			this.CheckType<T>();
			IMongoCollection<T> collection = this.GetCollection<T>();
			collection.InsertOne(model, null, default(CancellationToken));
		}

		public void InsertSync<T>(T model) where T : IBaseObject
		{
			this.CheckType<T>();
			IMongoCollection<T> collection = this.GetCollection<T>();
			collection.InsertOne(model, null, default(CancellationToken));
		}

		public async void BatchInsert<T>(IList<T> model) where T : IBaseObject
		{
			this.CheckType<T>();
			IMongoCollection<T> collection = this.GetCollection<T>();
			await collection.InsertManyAsync(model, null, default(CancellationToken));
		}

		public void BatchInsertSync<T>(IList<T> model) where T : IBaseObject
		{
			this.CheckType<T>();
			IMongoCollection<T> collection = this.GetCollection<T>();
			collection.InsertManyAsync(model, null, default(CancellationToken));
		}

		public async void Delete<T>(string key, object value) where T : IBaseObject
		{
			this.CheckType<T>();
			IMongoCollection<T> collection = this.GetCollection<T>();
			FilterDefinitionBuilder<T> filter = Builders<T>.Filter;
			FilterDefinition<T> filterDefinition = filter.Empty;
			string className = typeof(T).Name.ToLower();
			PropertyInfo propertyInfo = this.GetPropertyInfo<T>(className, key);
			if (propertyInfo != null)
			{
				filterDefinition &= filter.Eq<object>(propertyInfo.Name, value);
			}
			await collection.DeleteManyAsync(filterDefinition, default(CancellationToken));
		}

		public void DeleteSync<T>(string key, object value) where T : IBaseObject
		{
			this.CheckType<T>();
			IMongoCollection<T> collection = this.GetCollection<T>();
			FilterDefinitionBuilder<T> filter = Builders<T>.Filter;
			FilterDefinition<T> filterDefinition = filter.Empty;
			string className = typeof(T).Name.ToLower();
			PropertyInfo propertyInfo = this.GetPropertyInfo<T>(className, key);
			if (propertyInfo != null)
			{
				filterDefinition &= filter.Eq<object>(propertyInfo.Name, value);
			}
			collection.DeleteManyAsync(filterDefinition, default(CancellationToken));
		}

		public async void Update<T>(IList<string> keys, T model) where T : IBaseObject
		{
			this.CheckType<T>();
			PropertyInfo[] properties = model.GetType().GetProperties();
			IMongoCollection<T> collection = this.GetCollection<T>();
			FilterDefinitionBuilder<T> filter = Builders<T>.Filter;
			FilterDefinition<T> filterDefinition = filter.Empty;
			string className = typeof(T).Name.ToLower();
			PropertyInfo[] array;
			bool flag;
			foreach (string current in keys)
			{
				PropertyInfo propertyInfo = this.GetPropertyInfo<T>(className, current);
				if (propertyInfo != null)
				{
					array = properties;
					for (int i = 0; i < array.Length; i++)
					{
						PropertyInfo propertyInfo2 = array[i];
						if (propertyInfo2.Name.ToLower().Equals(current.ToLower()))
						{
							filterDefinition &= filter.Eq<object>(propertyInfo.Name, propertyInfo2.GetValue(model, null));
							break;
						}
					}
				}
			}
			UpdateDefinitionBuilder<T> update = Builders<T>.Update;
			UpdateDefinition<T> updateDefinition = null;
			array = model.GetType().GetProperties();
			for (int i = 0; i < array.Length; i++)
			{
				PropertyInfo propertyInfo2 = array[i];
				if (!propertyInfo2.Name.ToLower().Equals("id"))
				{
					if (updateDefinition == null)
					{
						updateDefinition = update.Set<object>(propertyInfo2.Name, propertyInfo2.GetValue(model, null));
					}
					else
					{
						updateDefinition = updateDefinition.Set(propertyInfo2.Name, propertyInfo2.GetValue(model, null));
					}
				}
			}
			await collection.UpdateOneAsync(filterDefinition, updateDefinition, null, default(CancellationToken));
		}

		public void UpdateSync<T>(IList<string> keys, T model) where T : IBaseObject
		{
			this.CheckType<T>();
			PropertyInfo[] properties = model.GetType().GetProperties();
			IMongoCollection<T> collection = this.GetCollection<T>();
			FilterDefinitionBuilder<T> filter = Builders<T>.Filter;
			FilterDefinition<T> filterDefinition = filter.Empty;
			string className = typeof(T).Name.ToLower();
			PropertyInfo[] array;
			foreach (string current in keys)
			{
				PropertyInfo propertyInfo = this.GetPropertyInfo<T>(className, current);
				if (propertyInfo != null)
				{
					array = properties;
					for (int i = 0; i < array.Length; i++)
					{
						PropertyInfo propertyInfo2 = array[i];
						if (propertyInfo2.Name.ToLower().Equals(current.ToLower()))
						{
							filterDefinition &= filter.Eq<object>(propertyInfo.Name, propertyInfo2.GetValue(model, null));
							break;
						}
					}
				}
			}
			UpdateDefinitionBuilder<T> update = Builders<T>.Update;
			UpdateDefinition<T> updateDefinition = null;
			array = model.GetType().GetProperties();
			for (int i = 0; i < array.Length; i++)
			{
				PropertyInfo propertyInfo2 = array[i];
				if (!propertyInfo2.Name.ToLower().Equals("id"))
				{
					if (updateDefinition == null)
					{
						updateDefinition = update.Set<object>(propertyInfo2.Name, propertyInfo2.GetValue(model, null));
					}
					else
					{
						updateDefinition = updateDefinition.Set(propertyInfo2.Name, propertyInfo2.GetValue(model, null));
					}
				}
			}
			collection.UpdateOneAsync(filterDefinition, updateDefinition, null, default(CancellationToken));
		}

		public void CreateTable<T>() where T : IBaseObject
		{
			this.CheckType<T>();
			string name = typeof(T).Name.ToLower();
			this.client.CreateCollectionAsync(name, null, default(CancellationToken));
		}

		public void DropTable<T>() where T : IBaseObject
		{
			this.CheckType<T>();
			string name = typeof(T).Name.ToLower();
			this.client.DropCollectionAsync(name, default(CancellationToken));
		}

		public void CreateIndex<T>(IndexField index) where T : IBaseObject
		{
			this.CheckType<T>();
			IMongoCollection<T> collection = this.GetCollection<T>();
			IndexKeysDefinition<T> keys = (index.Direction == OrderDirection.ASC) ? Builders<T>.IndexKeys.Ascending(index.Field) : Builders<T>.IndexKeys.Descending(index.Field);
			collection.Indexes.CreateOne(keys, null, default(CancellationToken));
		}

		public async void CreateIndexAsync<T>(IndexField index) where T : IBaseObject
		{
			this.CheckType<T>();
			IMongoCollection<T> collection = this.GetCollection<T>();
			IndexKeysDefinition<T> keys = (index.Direction == OrderDirection.ASC) ? Builders<T>.IndexKeys.Ascending(index.Field) : Builders<T>.IndexKeys.Descending(index.Field);
			await collection.Indexes.CreateOneAsync(keys, null, default(CancellationToken));
		}

		public void CreateIndexs<T>(IEnumerable<IndexField> indexs) where T : IBaseObject
		{
			this.CheckType<T>();
			if (indexs != null && indexs.Count<IndexField>() > 0)
			{
				IMongoCollection<T> collection = this.GetCollection<T>();
				List<CreateIndexModel<T>> list = new List<CreateIndexModel<T>>();
				foreach (IndexField current in indexs)
				{
					list.Add(new CreateIndexModel<T>((current.Direction == OrderDirection.ASC) ? Builders<T>.IndexKeys.Ascending(current.Field) : Builders<T>.IndexKeys.Descending(current.Field), null));
				}
				collection.Indexes.CreateMany(list, default(CancellationToken));
			}
		}

		public async void CreateIndexsAsync<T>(IEnumerable<IndexField> indexs) where T : IBaseObject
		{
			this.CheckType<T>();
			if (indexs != null && indexs.Count<IndexField>() > 0)
			{
				IMongoCollection<T> collection = this.GetCollection<T>();
				List<CreateIndexModel<T>> list = new List<CreateIndexModel<T>>();
				foreach (IndexField current in indexs)
				{
					list.Add(new CreateIndexModel<T>((current.Direction == OrderDirection.ASC) ? Builders<T>.IndexKeys.Ascending(current.Field) : Builders<T>.IndexKeys.Descending(current.Field), null));
				}
				await collection.Indexes.CreateManyAsync(list, default(CancellationToken));
			}
		}

		public void DropAllIndex<T>() where T : IBaseObject
		{
			this.CheckType<T>();
			IMongoCollection<T> collection = this.GetCollection<T>();
			collection.Indexes.DropAll(default(CancellationToken));
		}

		public async void DropAllIndexAsync<T>() where T : IBaseObject
		{
			this.CheckType<T>();
			IMongoCollection<T> collection = this.GetCollection<T>();
			await collection.Indexes.DropAllAsync(default(CancellationToken));
		}

		public IEnumerable<IndexField> GetIndexs<T>() where T : IBaseObject
		{
			this.CheckType<T>();
			List<IndexField> result = new List<IndexField>();
			IMongoCollection<T> collection = this.GetCollection<T>();
			using (IAsyncCursor<BsonDocument> asyncCursor = collection.Indexes.List(default(CancellationToken)))
			{
				List<BsonDocument> list = asyncCursor.ToList(default(CancellationToken));
				list.ForEach(delegate (BsonDocument M)
				{
					BsonElement bsonElement = M["key"].ToBsonDocument(null, null, default(BsonSerializationArgs)).Elements.ElementAt(0);
					result.Add(new IndexField
					{
						Field = bsonElement.Name,
						Direction = (bsonElement.Value.AsInt32 == 1) ? OrderDirection.ASC : OrderDirection.DESC
					});
				});
			}
			return result;
		}

		private IMongoCollection<T> GetCollection<T>() where T : IBaseObject
		{
			this.CheckType<T>();
			string key = typeof(T).Name.ToLower();
			if (!MongoDBClient.fields.ContainsKey(this.Config.ConnectionString))
			{
				lock (MongoDBClient._lock)
				{
					MongoDBClient.fields.Add(this.Config.ConnectionString, new Dictionary<string, IDictionary<string, PropertyInfo>>());
				}
			}
			if (!MongoDBClient.fields[this.Config.ConnectionString].ContainsKey(key))
			{
				lock (MongoDBClient._lock)
				{
					MongoDBClient.fields[this.Config.ConnectionString].Add(key, new Dictionary<string, PropertyInfo>());
				}
			}
			if (MongoDBClient.fields[this.Config.ConnectionString][key].Count <= 0)
			{
				lock (MongoDBClient._lock)
				{
					PropertyInfo[] properties = typeof(T).GetProperties();
					for (int i = 0; i < properties.Length; i++)
					{
						PropertyInfo propertyInfo = properties[i];
						if (!MongoDBClient.fields[this.Config.ConnectionString][key].ContainsKey(propertyInfo.Name.ToLower()))
						{
							MongoDBClient.fields[this.Config.ConnectionString][key].Add(propertyInfo.Name.ToLower(), propertyInfo);
						}
					}
				}
			}
			return this.client.GetCollection<T>(typeof(T).Name.ToLower(), null);
		}

		private PropertyInfo GetPropertyInfo<T>(string className, string propertyName) where T : IBaseObject
		{
			this.CheckType<T>();
			PropertyInfo propertyInfo;
			MongoDBClient.fields[this.Config.ConnectionString][className].TryGetValue(propertyName, out propertyInfo);
			PropertyInfo result;
			if (propertyInfo != null)
			{
				result = propertyInfo;
			}
			else if (MongoDBClient.fields[this.Config.ConnectionString][className].ContainsKey(propertyName.ToString().ToLower()))
			{
				PropertyInfo propertyInfo2 = MongoDBClient.fields[this.Config.ConnectionString][className][propertyName.ToString().ToLower()];
				result = propertyInfo2;
			}
			else
			{
				result = null;
			}
			return result;
		}

		public void Dispose()
		{
		}

		private void CheckType<T>()
		{
			if (!typeof(T).IsSubclassOf(typeof(MongoDBObject)))
			{
				throw new Exception("传入泛型未继承MongoDBObject类");
			}
		}
	}
}
