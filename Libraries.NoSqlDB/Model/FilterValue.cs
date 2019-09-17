namespace Libraries.NoSqlDB.Model
{
	public class FilterValue
	{
		private Operation operationType = Operation.AND;

		public string FieldName
		{
			get;
			set;
		}

		public object Value
		{
			get;
			set;
		}

		public FilterType FilterType
		{
			get;
			set;
		}

		public Operation OperationType
		{
			get
			{
				return this.operationType;
			}
			set
			{
				this.operationType = value;
			}
		}
	}
}
