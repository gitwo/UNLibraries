using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.FrameWork.Model
{
	public class PageList<T> : PageListBase<T>, IPageList<T>, IPageListBase<T>
	{
		public PageList()
		{
		}

		public PageList(IQueryable<T> source, int pageIndex, int pageSize) : base(source, pageIndex, pageSize)
		{
		}

		public PageList(IList<T> source, int pageIndex, int pageSize) : base(source, pageIndex, pageSize)
		{
		}

		public PageList(IEnumerable<T> source, int pageIndex, int pageSize, int totalCount) : base(source, pageIndex, pageSize, totalCount)
		{
		}
	}
}
