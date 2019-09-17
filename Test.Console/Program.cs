using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Console
{
	class Program
	{
		static void Main(string[] args)
		{
			ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("192.168.1.1:6379,password=123456");
			IDatabase db = redis.GetDatabase();
			long count = 10000 * 10;
			DateTime start = DateTime.Now;

			#region 所有Redis的数据写入方法
			db.StringSet("key_test", "shaocan");
			System.Console.WriteLine(db.StringGet("key_test"));
			//db.HashSet("userinfo", "name", "shaocan");
			//db.SetAdd("set_test", "user1");
			//db.SetAdd("set_test", "user2");
			//db.SortedSetAdd("sset_test", "user1", DateTime.Now.Ticks);
			//db.SortedSetAdd("sset_test", "user2", DateTime.Now.Ticks);
			//db.ListLeftPush("list_test", "user1");
			#endregion
			//start = DateTime.Now;

			////二进制格式
			//for (int i = 0; i < count; i++)
			//{
			//    User user = new User { Id = i, Name = "abc" + i, Age = 20 };
			//    string key = "myObject" + i;
			//    byte[] bytes;

			//    using (var stream = new MemoryStream())
			//    {
			//        new BinaryFormatter().Serialize(stream, user);
			//        bytes = stream.ToArray();
			//    }
			//    //设置值
			//    db.StringSet(key, bytes);
			//}


			////读取10w条数据
			//for (int i = 0; i < count; i++)
			//{
			//    string key = "myObject" + i;
			//    User user = null;
			//    byte[] bytes = (byte[])db.StringGet(key);

			//    if (bytes != null)
			//    {
			//        using (var stream = new MemoryStream(bytes))
			//        {
			//            //二进制流，反序列化
			//            user = (User)new BinaryFormatter().Deserialize(stream);
			//        }
			//    }
			//    Console.WriteLine(user.Name);
			//}
			//System.Console.WriteLine(string.Format("Binary Format {0} items takes {1} seconds", count, (DateTime.Now - start).TotalSeconds));
			//start = DateTime.Now;

			/* 100000 */
			//for (int i = 0; i < count; i++)
			//{
			//    User user = new User { Id = i, Name = "abc" + i, Age = 20 };
			//    string json = JsonConvert.SerializeObject(user);

			//    string key = "json" + i;
			//    db.StringSet(key, json);
			//}

			////读取10W条数据
			//for (int i = 0; i < count; i++)
			//{
			//    string key = "json" + i;
			//    string json = db.StringGet(key);
			//    User user = (User)JsonConvert.DeserializeObject(json, typeof(User));
			//    Console.WriteLine(user.Name);
			//}
			//System.Console.WriteLine(string.Format("JSON Format {0} items takes {1} seconds", count, (DateTime.Now - start).TotalSeconds));
			//start = DateTime.Now;


			////序列化DataSet为JSON。
			////http://www.newtonsoft.com/json/help/html/SerializeDataSet.htm
			//DataSet dataSet = new DataSet("dataSet");
			//dataSet.Namespace = "NetFrameWork";
			//DataTable table = new DataTable();
			//DataColumn idColumn = new DataColumn("id", typeof(int));
			//idColumn.AutoIncrement = true;

			//DataColumn itemColumn = new DataColumn("item");
			//table.Columns.Add(idColumn);
			//table.Columns.Add(itemColumn);
			//dataSet.Tables.Add(table);

			//for (int i = 0; i < 2; i++)
			//{
			//    DataRow newRow = table.NewRow();
			//    newRow["item"] = "[测试] " + i;
			//    table.Rows.Add(newRow);
			//}

			//dataSet.AcceptChanges();

			//string _json = JsonConvert.SerializeObject(dataSet, Formatting.Indented);
			////设置dataset1值
			//db.StringSet("dataset1", _json);

			//DataSet ds = (DataSet)JsonConvert.DeserializeObject(_json, typeof(DataSet));
			//Console.WriteLine(ds.Tables[0].Rows[0]["item"].ToString());

			System.Console.ReadLine();
		}
	}

	[Serializable]
	public class User
	{
		public long Id { get; set; }
		public string Name { get; set; }
		public int Age { get; set; }
	}
}
