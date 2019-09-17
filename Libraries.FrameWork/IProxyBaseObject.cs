using System;

namespace Libraries.FrameWork
{
	public interface IProxyBaseObject<T> : IDisposable
	{
		T Instance { get; }

		void Init(BaseConfig config = null);
	}
}
