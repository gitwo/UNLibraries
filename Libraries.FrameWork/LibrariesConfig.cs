using System;
using System.Configuration;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Libraries.FrameWork
{
	public class LibrariesConfig : IConfigurationSectionHandler
	{
		private static XmlNode Section
		{
			get;
			set;
		}

		public object Create(object parent, object configContext, XmlNode section)
		{
			Section = section;
			return new LibrariesConfig();
		}

		private T GetObjByXml<T>(XmlNode section, string nodeName) where T : new()
		{
			T result = (default(T) == null) ? Activator.CreateInstance<T>() : default(T);
			XmlNode xmlNode = section.SelectSingleNode(nodeName);
			if (xmlNode != null)
			{
				result = XmlDeserialize<T>(xmlNode.OuterXml);
			}
			return result;
		}

		public T GetObjByXml<T>(string nodeName) where T : new()
		{
			T result;
			if (Section == null)
			{
				result = default(T);
			}
			else
			{
				T t = (default(T) == null) ? Activator.CreateInstance<T>() : default(T);
				XmlNode xmlNode = Section.SelectSingleNode(nodeName);
				if (xmlNode != null)
				{
					t = XmlDeserialize<T>(xmlNode.OuterXml);
				}
				result = t;
			}
			return result;
		}

		private T XmlDeserialize<T>(string xmlData)
		{
			T result;
			try
			{
				XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
				using (TextReader textReader = new StringReader(xmlData))
				{
					T t = (T)xmlSerializer.Deserialize(textReader);
					result = t;
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
			return result;
		}


	}
}
