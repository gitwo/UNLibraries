using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Libraries.FrameWork.Model
{
	public interface IPageListBase<T>
	{
		IList<T> Items
		{
			get;
		}

		int PageIndex
		{
			get;
		}

		int PageSize
		{
			get;
		}

		int TotalCount
		{
			get;
		}

		int TotalPages
		{
			get;
		}

		bool HasPreviousPage
		{
			get;
		}

		bool HasNextPage
		{
			get;
		}
	}


	[DataContract]
	public class PageListBase<T> : IPageListBase<T>
	{
		[DataMember]
		public IList<T> Items
		{
			get;
			private set;
		}

		[DataMember]
		public int PageIndex
		{
			get;
			private set;
		}

		[DataMember]
		public int PageSize
		{
			get;
			private set;
		}

		[DataMember]
		public int TotalCount
		{
			get;
			private set;
		}

		[DataMember]
		public int TotalPages
		{
			get;
			private set;
		}

		[DataMember]
		public bool HasPreviousPage
		{
			get;
			private set;
		}

		[DataMember]
		public bool HasNextPage
		{
			get;
			private set;
		}

		public PageListBase()
		{
		}

		public PageListBase(IQueryable<T> source, int pageIndex, int pageSize)
		{
			int num = source.Count();
			TotalCount = num;
			TotalPages = num / pageSize;
			if (num % pageSize > 0)
			{
				TotalPages++;
			}
			PageSize = pageSize;
			PageIndex = pageIndex;
			Items = source.Skip(pageIndex * pageSize).Take(pageSize).ToList();
			HasNextPage = PageIndex + 1 < TotalPages;
			HasPreviousPage = PageIndex > 0;
		}

		public PageListBase(IList<T> source, int pageIndex, int pageSize)
		{
			TotalCount = source.Count<T>();
			TotalPages = TotalCount / pageSize;
			if (TotalCount % pageSize > 0)
			{
				TotalPages++;
			}
			PageSize = pageSize;
			PageIndex = pageIndex;
			Items = source.Skip(pageIndex * pageSize).Take(pageSize).ToList<T>();
			HasNextPage = (PageIndex + 1 < TotalPages);
			HasPreviousPage = (PageIndex > 0);
		}

		public PageListBase(IEnumerable<T> source, int pageIndex, int pageSize, int totalCount)
		{
			TotalCount = totalCount;
			TotalPages = TotalCount / pageSize;
			if (TotalCount % pageSize > 0)
			{
				TotalPages++;
			}
			PageSize = pageSize;
			PageIndex = pageIndex;
			Items = source.ToList();
			HasNextPage = PageIndex + 1 < TotalPages;
			HasPreviousPage = (PageIndex > 0);
		}
	}
}
