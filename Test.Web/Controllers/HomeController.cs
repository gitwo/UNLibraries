using Libraries.Cache;
using Libraries.FrameWork;
using Libraries.NoSqlDB;
using Libraries.NoSqlDB.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Test.Web.Models;

namespace Test.Web.Controllers
{
	public class HomeController : Controller
	{
		public ActionResult Index()
		{
			//创建缓存调用实例
			var provider = ClientProxy.GetInstance<ICacheProvider>();

			//保存
			// var data = new TestObj { ID = 1, Name = "Test1", Date = DateTime.Now, Available = true };
			// provider.Set<TestObj>("Test1", data, 1);

			//获取缓存值,若为空则代表不存在
			var data = provider.Get<Dictionary<string, string>>("Test1");
			// //缓存递增
			// var i = provider.Increment("testCount", 2);

			// //缓存递增
			// //i = provider.Decrement("testCount", 2);

			// //获取当前递增、递减后的缓存值
			//i = provider.GetCountVal("testCount");

			// //获取所有缓存Key(Memcached不支持)
			// var keyList = provider.GetAllKey();
			// //清空缓存
			// //provider.Clear();
			// var count = keyList[1];
			// return View(data);
			using (var client = ClientProxy.GetInstance<INoSqlDBProvider>())
			{
				client.Insert<MongoDBLog>(new MongoDBLog
				{
					SysId = 1,
					AppKey = "16031011",
					Level = 1,
					TargetObject = "web",
					TargetId = "001",
					Exception = "异常信息" + DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss.fff"),
					OperatorName = "admin1",
					Operator = 1,
					LogIp = "127.0.0.1",
					LogDate = DateTime.Now,
					Message = "测试信息"
				});
				//查询条件
				FilterCondition filter = new FilterCondition
				{
					Direction = OrderDirection.DESC,
					OrderBy = "Exception",
					PageIndex = 1,
					PageSize = 100
				};
				filter.AddField("LogIp", "127.0.0.1", FilterType.EQ);
				filter.AddField("Level", 1, FilterType.GTE);
				// filter.AddField("Level", 100, FilterType.LTE);
				//filter.AddField("Message", "试信", FilterType.Like);
				filter.AddField("Message", "测试信息", FilterType.Like);


				//查询
				var list = client.Query<MongoDBLog>(filter);

				//清除所有索引
				client.DropAllIndexAsync<MongoDBLog>();


				//创建单索引
				client.CreateIndex<MongoDBLog>(new IndexField() { Direction = OrderDirection.ASC, Field = "Exception" });


				//创建复合索引

				List<IndexField> indexs = new List<IndexField>();
				indexs.Add(new IndexField() { Direction = OrderDirection.DESC, Field = "Exception" });
				indexs.Add(new IndexField() { Direction = OrderDirection.ASC, Field = "Level" });
				client.CreateIndexs<MongoDBLog>(indexs);


				//查询创建的索引
				var indexList = client.GetIndexs<MongoDBLog>();

				//删除数据
				client.Delete<MongoDBLog>("Level", 69);
				client.DeleteSync<MongoDBLog>("Level", 68);
			}
			return View();
		}

		public ActionResult About()
		{
			ViewBag.Message = "Your application description page.";

			return View();
		}

		public ActionResult Contact()
		{
			ViewBag.Message = "Your contact page.";

			return View();
		}
	}



	[Serializable]
	public class TestObj
	{
		public int ID { get; set; }
		public string Name { get; set; }
		public DateTime Date { get; set; }
		public bool Available { get; set; }
	}

	
}