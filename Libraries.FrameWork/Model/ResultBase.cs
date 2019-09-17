using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.FrameWork.Model
{
	public class ResultBase
	{
		public bool IsSucc
		{
			get;
			set;
		}

		public string ErrCode
		{
			get;
			set;
		}

		public string ErrMsg
		{
			get;
			set;
		}

		public string DetailError
		{
			get;
			set;
		}
	}
}
