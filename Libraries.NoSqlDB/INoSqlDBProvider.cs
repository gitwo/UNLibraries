using Libraries.FrameWork;
using Libraries.FrameWork.Model;
using Libraries.NoSqlDB.Model;
using System;
using System.Collections.Generic;

namespace Libraries.NoSqlDB
{
	public interface INoSqlDBProvider : IProxyBaseObject<INoSqlDBProvider>, IDisposable
	{
		IPageList<T> Query<T>(FilterCondition filter) where T : IBaseObject;

		void Insert<T>(T model) where T : IBaseObject;

		void InsertSync<T>(T model) where T : IBaseObject;

		void BatchInsert<T>(IList<T> model) where T : IBaseObject;

		void BatchInsertSync<T>(IList<T> model) where T : IBaseObject;

		void Delete<T>(string key, object value) where T : IBaseObject;

		void DeleteSync<T>(string key, object value) where T : IBaseObject;

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
