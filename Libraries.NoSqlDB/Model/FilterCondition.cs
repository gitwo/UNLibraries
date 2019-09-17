using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.NoSqlDB.Model
{
	public class FilterCondition
	{

		private IList<FilterValue> fields = new List<FilterValue>();

		private int pageIndex = 0;

		[Description("当前页索引")]
		public int PageIndex
		{
			get
			{
				return this.pageIndex;
			}
			set
			{
				if (value > 0)
				{
					this.pageIndex = value;
				}
				else
				{
					this.pageIndex = 1;
				}
			}
		}

		[Description("每页大小")]
		public int PageSize
		{
			get;
			set;
		}

		[Description("排序列名")]
		public string OrderBy
		{
			get;
			set;
		}

		[Description("排序方向")]
		public OrderDirection Direction
		{
			get;
			set;
		}

		[Description("列的过滤类型")]
		public IList<FilterValue> Fields
		{
			get
			{
				return this.fields;
			}
		}

		public void AddField(string key, object value, FilterType type, Operation operationType = Operation.AND)
		{
			this.fields.Add(new FilterValue
			{
				FieldName = key,
				FilterType = type,
				Value = value,
				OperationType = operationType
			});
		}

	}
}
