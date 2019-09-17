using Libraries.NoSqlDB.Model;
using System;

namespace Test.Web.Models
{
	[Serializable]
	public class MongoDBLog : MongoDBObject
	{
		public int SysId { get; set; }
		public string AppKey { get; set; }
		public int Level { get; set; }
		public string TargetObject { get; set; }
		public string TargetId { get; set; }
		public string Exception { get; set; }
		public string OperatorName { get; set; }
		public int Operator { get; set; }
		public string LogIp { get; set; }
		public DateTime LogDate { get; set; }
		public string Message { get; set; }
	}
}