namespace Libraries.NoSqlDB.Model
{
	public enum FilterType
	{
		LTE = 1,
		GTE,
		LT,
		GT,
		EQ,
		Like,
		Or
	}

	public enum Operation
	{
		AND = 1,
		OR
	}
	public enum OrderDirection
	{
		ASC = 1,
		DESC
	}
}
