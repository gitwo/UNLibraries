using Libraries.FrameWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.NoSqlDB.Model
{
	public class NoSqlDBConfig: BaseConfig
	{
		public string ConnectionString
		{
			get;
			set;
		}

		public string DBName
		{
			get;
			set;
		}
	}
}
