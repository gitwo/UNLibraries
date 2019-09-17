using Libraries.FrameWork.Model;
using Libraries.NoSqlDB.Model;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.NoSqlDB
{
	internal interface INoSqlDBClient : IDisposable
	{
		IMongoDatabase Client
		{
			get;
		}

		NoSqlDBConfig Config
		{
			get;
		}

		INoSqlDBClient Connect(NoSqlDBConfig config);

		IPageList<T> Query<T>(FilterCondition filter) where T : IBaseObject;

		void Insert<T>(T model) where T : IBaseObject;

		void InsertSync<T>(T model) where T : IBaseObject;

		void BatchInsert<T>(IList<T> model) where T : IBaseObject;

		void BatchInsertSync<T>(IList<T> model) where T : IBaseObject;

		void Delete<T>(string key, object value) where T : IBaseObject;

		void DeleteSync<T>(string key, object value) where T : IBaseObject;

		void Update<T>(IList<string> keys, T model) where T : IBaseObject;

		void UpdateSync<T>(IList<string> keys, T model) where T : IBaseObject;

		void CreateTable<T>() where T : IBaseObject;

		void DropTable<T>() where T : IBaseObject;

		void CreateIndex<T>(IndexField index) where T : IBaseObject;

		void CreateIndexAsync<T>(IndexField index) where T : IBaseObject;

		void CreateIndexs<T>(IEnumerable<IndexField> indexs) where T : IBaseObject;

		void CreateIndexsAsync<T>(IEnumerable<IndexField> indexs) where T : IBaseObject;

		void DropAllIndex<T>() where T : IBaseObject;

		void DropAllIndexAsync<T>() where T : IBaseObject;

		IEnumerable<IndexField> GetIndexs<T>() where T : IBaseObject;
	}
}
