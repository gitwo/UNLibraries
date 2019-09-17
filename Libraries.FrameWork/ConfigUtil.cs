using System.Configuration;

namespace Libraries.FrameWork
{
	public class ConfigUtil
	{
		public static T GetConfig<T>(string configPath = "") where T : new()
		{
			string nodeName = (!string.IsNullOrEmpty(configPath)) ? configPath : typeof(T).Name;
			T result = default(T);
			LibrariesConfig librariesConfig = ConfigurationManager.GetSection("LibrariesConfig") as LibrariesConfig;
			if (librariesConfig != null)
			{
				result = librariesConfig.GetObjByXml<T>(nodeName);
			}
			return result;
		}
	}
}
