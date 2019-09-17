namespace Libraries.FrameWork.Model
{
	public class PageFilterBase
	{
		private int _pageSize;

		private int _pageIndex;

		public int PageSize
		{
			get
			{
				if (_pageSize == 0)
				{
					_pageSize = 10;
				}
				return _pageSize;
			}
			set
			{
				_pageSize = value;
			}
		}

		public int PageIndex
		{
			get
			{
				return (_pageIndex > 0) ? _pageIndex : 1;
			}
			set
			{
				_pageIndex = value;
			}
		}
	}
	
}
