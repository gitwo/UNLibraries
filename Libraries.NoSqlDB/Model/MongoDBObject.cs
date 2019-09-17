using MongoDB.Bson;

namespace Libraries.NoSqlDB.Model
{
	public class MongoDBObject : IBaseObject
	{
		public ObjectId Id
		{
			get;
			set;
		}

		public string GetSharedKey()
		{
			return this.Id.ToString();
		}
	}

}
