#define DEBUG
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;


namespace Libraries.Cache.Redis
{
	/// <summary>
	/// 直接操作Redis库
	/// </summary>
	public class Redis : IDisposable
	{
		private Socket socket;
		private BufferedStream bstream;

		public enum KeyType
		{
			None,
			String,
			List,
			Set
		}

		public class ResponseException : Exception
		{
			public ResponseException(string code) : base("响应错误")
			{
				Code = code;
			}

			public string Code { get; private set; }
		}

		public Redis(string host, int port)
		{
			if (host == null)
			{
				throw new ArgumentNullException("host");
			}

			Host = host;
			Port = port;
			SendTimeout = -1;
		}

		public Redis(string host) : this(host, 6379)
		{
		}

		public Redis() : this("localhost", 6379)
		{
		}

		public string Host { get; private set; }
		public int Port { get; private set; }
		public int RetryTimeout { get; set; }
		public int RetryCount { get; set; }
		public int SendTimeout { get; set; }
		public string Password { get; set; }

		private int db;

		/// <summary>
		/// SELECT index 默认使用 0 号数据库
		/// 切换到指定的数据库，数据库索引号 index 用数字值指定，以 0 作为起始索引值。
		/// </summary>
		public int Db
		{
			get { return db; }

			set
			{
				db = value;
				SendExpectSuccess("SELECT", db);
			}
		}

		public string this[string key]
		{
			get { return GetString(key); }
			set { Set(key, value); }
		}

		/// <summary>
		/// SET key value [EX seconds] [PX milliseconds] [NX|XX]
		/// 将字符串值 value 关联到 key 。
		/// 如果 key 已经持有其他值， SET 就覆写旧值，无视类型。
		/// 对于某个原本带有生存时间（TTL）的键来说， 当 SET 命令成功在这个键上执行时， 这个键原有的 TTL 将被清除。
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void Set(string key, string value)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			if (value == null)
				throw new ArgumentNullException("value");

			Set(key, Encoding.UTF8.GetBytes(value));
		}

		public void Set(string key, byte[] value)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}

			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			if (value.Length > 1073741824)
			{
				throw new ArgumentException("value exceeds 1G", "value");
			}

			if (!SendDataCommand(value, "SET", key))
			{
				throw new Exception("Unable to connect");
			}

			ExpectSuccess();
		}

		/// <summary>
		/// SETNX key value 『SET if Not eXists』(如果不存在，则 SET)
		/// 将 key 的值设为value ，当且仅当 key 不存在。
		/// 若给定的 key已经存在，则 SETNX 不做任何动作。
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool SetNX(string key, string value)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			if (value == null)
				throw new ArgumentNullException("value");

			return SetNX(key, Encoding.UTF8.GetBytes(value));
		}

		public bool SetNX(string key, byte[] value)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			if (value == null)
				throw new ArgumentNullException("value");

			if (value.Length > 1073741824)
				throw new ArgumentException("value exceeds 1G", "value");

			return SendDataExpectInt(value, "SETNX", key) > 0 ? true : false;
		}

		public void Set(IDictionary<string, string> dict)
		{
			if (dict == null)
				throw new ArgumentNullException("dict");

			Set(dict.ToDictionary(k => k.Key, v => Encoding.UTF8.GetBytes(v.Value)));
		}

		public void Set(IDictionary<string, byte[]> dict)
		{
			if (dict == null)
				throw new ArgumentNullException("dict");

			MSet(dict.Keys.ToArray(), dict.Values.ToArray());
		}

		/// <summary>
		/// MSET key value [key value ...]  同时设置一个或多个 key-value 对。
		/// 如果某个给定 key 已经存在，那么 MSET 会用新值覆盖原来的旧值，
		/// 如果这不是你所希望的效果，请考虑使用 MSETNX 命令：它只会在所有给定 key 都不存在的情况下进行设置操作。
		/// MSET 是一个原子性(atomic)操作，所有给定 key 都会在同一时间内被设置，某些给定 key 被更新而另一些给定 key 没有改变的情况，不可能发生。
		/// </summary>
		/// <param name="keys"></param>
		/// <param name="values"></param>

		public void MSet(string[] keys, byte[][] values)
		{
			if (keys.Length != values.Length)
				throw new ArgumentException("keys and values must have the same size");

			byte[] nl = Encoding.UTF8.GetBytes("\r\n");
			var ms = new MemoryStream();

			for (int i = 0; i < keys.Length; i++)
			{
				byte[] key = Encoding.UTF8.GetBytes(keys[i]);
				byte[] val = values[i];
				byte[] kLength = Encoding.UTF8.GetBytes("$" + key.Length + "\r\n");
				byte[] k = Encoding.UTF8.GetBytes(keys[i] + "\r\n");
				byte[] vLength = Encoding.UTF8.GetBytes("$" + val.Length + "\r\n");
				ms.Write(kLength, 0, kLength.Length);
				ms.Write(k, 0, k.Length);
				ms.Write(vLength, 0, vLength.Length);
				ms.Write(val, 0, val.Length);
				ms.Write(nl, 0, nl.Length);
			}

			SendDataRESP(ms.ToArray(), "*" + (keys.Length * 2 + 1) + "\r\n$4\r\nMSET\r\n");
			ExpectSuccess();
		}

		/// <summary>
		/// GET key 返回 key 所关联的字符串值。
		/// 如果 key 不存在那么返回特殊值 nil 。
		/// 假如 key 储存的值不是字符串类型，返回一个错误，因为 GET 只能用于处理字符串值。
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>		
		public byte[] Get(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectData("GET", key);
		}

		public string GetString(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return Encoding.UTF8.GetString(Get(key));
		}

		/// <summary>
		/// SORT key [BY pattern] [LIMIT offset count] [GET pattern [GET pattern ...]] [ASC | DESC] [ALPHA] [STORE destination]
		/// 返回或保存给定列表、集合、有序集合 key 中经过排序的元素。
		/// 排序默认以数字作为对象，值被解释为双精度浮点数，然后进行比较。
		/// 最简单的 SORT 使用方法是 SORT key 返回键值从小到大排序的结果 和 SORT key DESC 返回键值从大到小排序的结果
		/// </summary>
		/// <param name="options"></param>
		/// <returns></returns>
		public byte[][] Sort(SortOptions options)
		{
			return Sort(options.Key, options.StoreInKey, options.ToArgs());
		}

		public byte[][] Sort(string key, string destination, params object[] options)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			int offset = string.IsNullOrEmpty(destination) ? 1 : 3;
			var args = new object[offset + options.Length];

			args[0] = key;
			Array.Copy(options, 0, args, offset, options.Length);
			if (offset == 1)
			{
				return SendExpectDataArray("SORT", args);
			}
			args[1] = "STORE";
			args[2] = destination;
			int n = SendExpectInt("SORT", args);
			return new byte[n][];
		}

		/// <summary>
		/// GETSET key value  将给定 key 的值设为 value ，并返回 key 的旧值(old value)。
		/// 当 key 存在但不是字符串类型时，返回一个错误。
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public byte[] GetSet(string key, byte[] value)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			if (value == null)
				throw new ArgumentNullException("value");

			if (value.Length > 1073741824)
				throw new ArgumentException("value exceeds 1G", "value");

			if (!SendDataCommand(value, "GETSET", key))
				throw new Exception("Unable to connect");

			return ReadData();
		}

		public string GetSet(string key, string value)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			if (value == null)
				throw new ArgumentNullException("value");
			return Encoding.UTF8.GetString(GetSet(key, Encoding.UTF8.GetBytes(value)));
		}

		private string ReadLine()
		{
			var sb = new StringBuilder();
			int c;

			while ((c = bstream.ReadByte()) != -1)
			{
				if (c == '\r')
					continue;
				if (c == '\n')
					break;
				sb.Append((char)c);
			}
			return sb.ToString();
		}

		private void Connect()
		{
			socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.NoDelay = true;
			socket.SendTimeout = SendTimeout;
			socket.Connect(Host, Port);
			if (!socket.Connected)
			{
				socket.Close();
				socket = null;
				return;
			}
			bstream = new BufferedStream(new NetworkStream(socket), 16 * 1024);

			if (Password != null)
			{
				//AUTH password
				//通过设置配置文件中 requirepass 项的值(使用命令 CONFIG SET requirepass password )，可以使用密码来保护 Redis 服务器。
				//如果开启了密码保护的话，在每次连接 Redis 服务器之后，就要使用 AUTH 命令解锁，解锁之后才能使用其他 Redis 命令。
				//如果 AUTH 命令给定的密码 password 和配置文件中的密码相符的话，服务器会返回 OK 并开始接受命令输入。
				//另一方面，假如密码不匹配的话，服务器将返回一个错误，并要求客户端需重新输入密码。
				SendExpectSuccess("AUTH", Password);
			}
		}

		private readonly byte[] end_data = { (byte)'\r', (byte)'\n' };

		private bool SendDataCommand(byte[] data, string cmd, params object[] args)
		{
			string resp = "*" + (1 + args.Length + 1) + "\r\n";
			resp += "$" + cmd.Length + "\r\n" + cmd + "\r\n";
			foreach (object arg in args)
			{
				string argStr = arg.ToString();
				int argStrLength = Encoding.UTF8.GetByteCount(argStr);
				resp += "$" + argStrLength + "\r\n" + argStr + "\r\n";
			}
			resp += "$" + data.Length + "\r\n";

			return SendDataRESP(data, resp);
		}

		private bool SendDataRESP(byte[] data, string resp)
		{
			if (socket == null)
				Connect();
			if (socket == null)
				return false;

			byte[] r = Encoding.UTF8.GetBytes(resp);
			try
			{
				Log("C", resp);
				socket.Send(r);
				if (data != null)
				{
					socket.Send(data);
					socket.Send(end_data);
				}
			}
			catch (SocketException)
			{
				// timeout;
				socket.Close();
				socket = null;

				return false;
			}
			return true;
		}

		private bool SendCommand(string cmd, params object[] args)
		{
			if (socket == null)
			{
				Connect(); //建立连接redis
			}
			if (socket == null)
			{
				return false;
			}

			string resp = "*" + (1 + args.Length) + "\r\n";
			resp += "$" + cmd.Length + "\r\n" + cmd + "\r\n";
			foreach (object arg in args)
			{
				string argStr = arg.ToString();
				int argStrLength = Encoding.UTF8.GetByteCount(argStr);
				resp += "$" + argStrLength + "\r\n" + argStr + "\r\n";
			}

			byte[] r = Encoding.UTF8.GetBytes(resp);
			try
			{
				Log("C", resp);
				socket.Send(r);
			}
			catch (SocketException)
			{
				// timeout;
				socket.Close();
				socket = null;

				return false;
			}
			return true;
		}

		[Conditional("DEBUG")]
		private void Log(string id, string message)
		{
			Console.WriteLine(id + ": " + message.Trim().Replace("\r\n", " "));
		}

		//期待成功
		private void ExpectSuccess()
		{
			int c = bstream.ReadByte();
			if (c == -1)
			{
				throw new ResponseException("No more data");
			}
			string s = ReadLine();
			Log("S", (char)c + s);
			if (c == '-')
				throw new ResponseException(s.StartsWith("ERR ") ? s.Substring(4) : s);
		}

		private void SendExpectSuccess(string cmd, params object[] args)
		{
			if (!SendCommand(cmd, args))
			{
				throw new Exception("Unable to connect");
			}

			ExpectSuccess();
		}

		private int SendDataExpectInt(byte[] data, string cmd, params object[] args)
		{
			if (!SendDataCommand(data, cmd, args))
				throw new Exception("Unable to connect");

			int c = bstream.ReadByte();
			if (c == -1)
				throw new ResponseException("No more data");

			string s = ReadLine();
			Log("S", (char)c + s);
			if (c == '-')
				throw new ResponseException(s.StartsWith("ERR ") ? s.Substring(4) : s);
			if (c == ':')
			{
				int i;
				if (int.TryParse(s, out i))
					return i;
			}
			throw new ResponseException("Unknown reply on integer request: " + c + s);
		}

		private int SendExpectInt(string cmd, params object[] args)
		{
			if (!SendCommand(cmd, args))
				throw new Exception("Unable to connect");

			int c = bstream.ReadByte();
			if (c == -1)
				throw new ResponseException("No more data");

			string s = ReadLine();
			Log("S", (char)c + s);
			if (c == '-')
				throw new ResponseException(s.StartsWith("ERR ") ? s.Substring(4) : s);
			if (c == ':')
			{
				int i;
				if (int.TryParse(s, out i))
					return i;
			}
			throw new ResponseException("Unknown reply on integer request: " + c + s);
		}

		private string SendExpectString(string cmd, params object[] args)
		{
			if (!SendCommand(cmd, args))
				throw new Exception("Unable to connect");

			int c = bstream.ReadByte();
			if (c == -1)
				throw new ResponseException("No more data");

			string s = ReadLine();
			Log("S", (char)c + s);
			if (c == '-')
				throw new ResponseException(s.StartsWith("ERR ") ? s.Substring(4) : s);
			if (c == '+')
				return s;

			throw new ResponseException("Unknown reply on integer request: " + c + s);
		}

		//
		// 这一个不抛出错误
		//
		private string SendGetString(string cmd, params object[] args)
		{
			if (!SendCommand(cmd, args))
				throw new Exception("Unable to connect");

			return ReadLine();
		}

		private byte[] SendExpectData(string cmd, params object[] args)
		{
			if (!SendCommand(cmd, args))
				throw new Exception("Unable to connect");

			return ReadData();
		}

		private byte[] ReadData()
		{
			string s = ReadLine();
			Log("S", s);
			if (s.Length == 0)
				throw new ResponseException("Zero length respose");

			char c = s[0];
			if (c == '-')
				throw new ResponseException(s.StartsWith("-ERR ") ? s.Substring(5) : s.Substring(1));

			if (c == '$')
			{
				if (s == "$-1")
					return null;
				int n;

				if (Int32.TryParse(s.Substring(1), out n))
				{
					var retbuf = new byte[n];

					int bytesRead = 0;
					do
					{
						int read = bstream.Read(retbuf, bytesRead, n - bytesRead);
						if (read < 1)
							throw new ResponseException("Invalid termination mid stream");
						bytesRead += read;
					} while (bytesRead < n);
					if (bstream.ReadByte() != '\r' || bstream.ReadByte() != '\n')
						throw new ResponseException("Invalid termination");
					return retbuf;
				}
				throw new ResponseException("Invalid length");
			}

			/* don't treat arrays here because only one element works -- use DataArray!
			//returns the number of matches
			if (c == '*') {
				int n;
				if (Int32.TryParse(s.Substring(1), out n)) 
					return n <= 0 ? new byte [0] : ReadData();

				throw new ResponseException ("Unexpected length parameter" + r);
			}
			*/

			throw new ResponseException("Unexpected reply: " + s);
		}


		//EXISTS key
		//检查给定 key 是否存在。
		public bool ContainsKey(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("EXISTS", key) == 1;
		}

		//DEL key [key ...]
		//删除给定的一个或多个 key 。
		//不存在的 key 会被忽略。
		public bool Remove(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("DEL", key) == 1;
		}

		public int Remove(params string[] args)
		{
			if (args == null)
				throw new ArgumentNullException("args");
			return SendExpectInt("DEL", args);
		}


		//INCR key
		//将 key 中储存的数字值增一。
		//如果 key 不存在，那么 key 的值会先被初始化为 0 ，然后再执行 INCR 操作。
		//如果值包含错误的类型，或字符串类型的值不能表示为数字，那么返回一个错误。
		//本操作的值限制在 64 位(bit)有符号数字表示之内。
		//这是一个针对字符串的操作，因为 Redis 没有专用的整数类型，所以 key 内储存的字符串被解释为十进制 64 位有符号整数来执行 INCR 操作。 
		public int Increment(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("INCR", key);
		}


		//INCRBY key increment
		//将 key 所储存的值加上增量 increment 。
		//如果 key 不存在，那么 key 的值会先被初始化为 0 ，然后再执行 INCRBY 命令。
		//如果值包含错误的类型，或字符串类型的值不能表示为数字，那么返回一个错误。
		//本操作的值限制在 64 位(bit)有符号数字表示之内。

		public int Increment(string key, int count)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("INCRBY", key, count);
		}


		//    DECR key
		//将 key 中储存的数字值减一。
		//如果 key 不存在，那么 key 的值会先被初始化为 0 ，然后再执行 DECR 操作。
		//如果值包含错误的类型，或字符串类型的值不能表示为数字，那么返回一个错误。
		//本操作的值限制在 64 位(bit)有符号数字表示之内。
		public int Decrement(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("DECR", key);
		}


		//DECRBY key decrement
		//将 key 所储存的值减去减量 decrement 。
		//如果 key 不存在，那么 key 的值会先被初始化为 0 ，然后再执行 DECRBY 操作。
		//如果值包含错误的类型，或字符串类型的值不能表示为数字，那么返回一个错误。
		//本操作的值限制在 64 位(bit)有符号数字表示之内。

		public int Decrement(string key, int count)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("DECRBY", key, count);
		}

		//TYPE key
		//返回 key 所储存的值的类型。
		public KeyType TypeOf(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			switch (SendExpectString("TYPE", key))
			{
				case "none":
					return KeyType.None;
				case "string":
					return KeyType.String;
				case "set":
					return KeyType.Set;
				case "list":
					return KeyType.List;
			}
			throw new ResponseException("Invalid value");
		}


		//RANDOMKEY
		//从当前数据库中随机返回(不删除)一个 key 。
		public string RandomKey()
		{
			return SendExpectString("RANDOMKEY");
		}

		//RENAME key newkey
		//将 key 改名为 newkey 。
		//当 key 和 newkey 相同，或者 key 不存在时，返回一个错误。
		//当 newkey 已经存在时， RENAME 命令将覆盖旧值。
		public bool Rename(string oldKeyname, string newKeyname)
		{
			if (oldKeyname == null)
				throw new ArgumentNullException("oldKeyname");
			if (newKeyname == null)
				throw new ArgumentNullException("newKeyname");
			return SendGetString("RENAME", oldKeyname, newKeyname)[0] == '+';
		}


		// EXPIRE key seconds
		//为给定 key 设置生存时间，当 key 过期时(生存时间为 0 )，它会被自动删除。
		//在 Redis 中，带有生存时间的 key 被称为『易失的』(volatile)。
		//生存时间可以通过使用 DEL 命令来删除整个 key 来移除，或者被 SET 和 GETSET 命令覆写(overwrite)，这意味着，如果一个命令只是修改(alter)一个带生存时间的 key 的值而不是用一个新的 key 值来代替(replace)它的话，那么生存时间不会被改变。
		//比如说，对一个 key 执行 INCR 命令，对一个列表进行 LPUSH 命令，或者对一个哈希表执行 HSET 命令，这类操作都不会修改 key 本身的生存时间。
		//另一方面，如果使用 RENAME 对一个 key 进行改名，那么改名后的 key 的生存时间和改名前一样。
		//RENAME 命令的另一种可能是，尝试将一个带生存时间的 key 改名成另一个带生存时间的 another_key ，这时旧的 another_key (以及它的生存时间)会被删除，然后旧的 key 会改名为 another_key ，因此，新的 another_key 的生存时间也和原本的 key 一样。
		//使用 PERSIST 命令可以在不删除 key 的情况下，移除 key 的生存时间，让 key 重新成为一个『持久的』(persistent) key 。
		//更新生存时间
		//可以对一个已经带有生存时间的 key 执行 EXPIRE 命令，新指定的生存时间会取代旧的生存时间。
		//过期时间的精确度
		//在 Redis 2.4 版本中，过期时间的延迟在 1 秒钟之内 —— 也即是，就算 key 已经过期，但它还是可能在过期之后一秒钟之内被访问到，而在新的 Redis 2.6 版本中，延迟被降低到 1 毫秒之内。
		//Redis 2.1.3 之前的不同之处
		//在 Redis 2.1.3 之前的版本中，修改一个带有生存时间的 key 会导致整个 key 被删除，这一行为是受当时复制(replication)层的限制而作出的，现在这一限制已经被修复。
		public bool Expire(string key, int seconds)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("EXPIRE", key, seconds) == 1;
		}

		//EXPIREAT key timestamp
		//EXPIREAT 的作用和 EXPIRE 类似，都用于为 key 设置生存时间。
		//不同在于 EXPIREAT 命令接受的时间参数是 UNIX 时间戳(unix timestamp)。
		public bool ExpireAt(string key, int time)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("EXPIREAT", key, time) == 1;
		}

		//TTL key
		//以秒为单位，返回给定 key 的剩余生存时间(TTL, time to live)。
		public int TimeToLive(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("TTL", key);
		}

		//DBSIZE
		//返回当前数据库的 key 的数量。
		public int DbSize
		{
			get { return SendExpectInt("DBSIZE"); }
		}


		//SAVE 命令执行一个同步保存操作，将当前 Redis 实例的所有数据快照(snapshot)以 RDB 文件的形式保存到硬盘。
		//一般来说，在生产环境很少执行 SAVE 操作，因为它会阻塞所有客户端，保存数据库的任务通常由 BGSAVE 命令异步地执行。
		//然而，如果负责保存数据的后台子进程不幸出现问题时， SAVE 可以作为保存数据的最后手段来使用。
		public void Save()
		{
			SendExpectSuccess("SAVE");
		}

		// 在后台异步(Asynchronously)保存当前数据库的数据到磁盘。
		//BGSAVE 命令执行之后立即返回 OK ，然后 Redis fork 出一个新子进程，原来的 Redis 进程(父进程)继续处理客户端请求，而子进程则负责将数据保存到磁盘，然后退出。
		//客户端可以通过 LASTSAVE 命令查看相关信息，判断 BGSAVE 命令是否执行成功。
		public void BackgroundSave()
		{
			SendExpectSuccess("BGSAVE");
		}


		//SHUTDOWN 命令执行以下操作：
		//停止所有客户端
		//如果有至少一个保存点在等待，执行 SAVE 命令
		//如果 AOF 选项被打开，更新 AOF 文件
		//关闭 redis 服务器(server)

		//如果持久化被打开的话， SHUTDOWN 命令会保证服务器正常关闭而不丢失任何数据。
		//另一方面，假如只是单纯地执行 SAVE 命令，然后再执行 QUIT 命令，则没有这一保证 —— 因为在执行 SAVE 之后、执行 QUIT 之前的这段时间中间，
		//其他客户端可能正在和服务器进行通讯，这时如果执行 QUIT 就会造成数据丢失。

		//SAVE 和 NOSAVE 修饰符
		//通过使用可选的修饰符，可以修改 SHUTDOWN 命令的表现。比如说：
		//执行 SHUTDOWN SAVE 会强制让数据库执行保存操作，即使没有设定(configure)保存点
		//执行 SHUTDOWN NOSAVE 会阻止数据库执行保存操作，即使已经设定有一个或多个保存点(你可以将这一用法看作是强制停止服务器的一个假想的 ABORT 命令)
		public void Shutdown()
		{
			SendCommand("SHUTDOWN");
			try
			{
				// the server may return an error
				string s = ReadLine();
				Log("S", s);
				if (s.Length == 0)
					throw new ResponseException("Zero length respose");
				throw new ResponseException(s.StartsWith("-ERR ") ? s.Substring(5) : s.Substring(1));
			}
			catch (IOException)
			{
				//这是预期的正确的结果
				socket.Close();
				socket = null;
			}
		}

		//FLUSHALL
		//清空整个 Redis 服务器的数据(删除所有数据库的所有 key )。
		//此命令从不失败。
		public void FlushAll()
		{
			SendExpectSuccess("FLUSHALL");
		}

		//FLUSHDB
		//清空当前数据库中的所有 key。
		//此命令从不失败。
		public void FlushDb()
		{
			SendExpectSuccess("FLUSHDB");
		}

		private const long UnixEpoch = 621355968000000000L;

		//返回最近一次 Redis 成功将数据保存到磁盘上的时间，以 UNIX 时间戳格式表示。
		public DateTime LastSave
		{
			get
			{
				int t = SendExpectInt("LASTSAVE");

				return new DateTime(UnixEpoch) + TimeSpan.FromSeconds(t);
			}
		}

		//INFO [section]
		//以一种易于解释（parse）且易于阅读的格式，返回关于 Redis 服务器的各种信息和统计数值。
		public Dictionary<string, string> GetInfo()
		{
			byte[] r = SendExpectData("INFO");
			var dict = new Dictionary<string, string>();

			foreach (string line in Encoding.UTF8.GetString(r).Split('\n'))
			{
				int p = line.IndexOf(':');
				if (p == -1)
					continue;
				dict.Add(line.Substring(0, p), line.Substring(p + 1));
			}
			return dict;
		}


		//KEYS pattern

		//查找所有符合给定模式 pattern 的 key 。
		//KEYS * 匹配数据库中所有 key 。
		//KEYS h?llo 匹配 hello ， hallo 和 hxllo 等。
		//KEYS h*llo 匹配 hllo 和 heeeeello 等。
		//KEYS h[ae]llo 匹配 hello 和 hallo ，但不匹配 hillo 。

		//特殊符号用 \ 隔开
		//KEYS 的速度非常快，但在一个大的数据库中使用它仍然可能造成性能问题，如果你需要从一个数据集中查找特定的 key ，你最好还是用 Redis 的集合结构(set)来代替。 
		public string[] Keys
		{
			get { return GetKeys("*"); }
		}

		public string[] GetKeys(string pattern)
		{
			if (pattern == null)
				throw new ArgumentNullException("pattern");

			return SendExpectStringArray("KEYS", pattern);
		}

		//MGET key [key ...]
		//返回所有(一个或多个)给定 key 的值。
		//如果给定的 key 里面，有某个 key 不存在，那么这个 key 返回特殊值 nil 。因此，该命令永不失败。
		public byte[][] MGet(params string[] keys)
		{
			if (keys == null)
				throw new ArgumentNullException("keys");
			if (keys.Length == 0)
				throw new ArgumentException("keys");

			return SendExpectDataArray("MGET", keys);
		}


		public string[] SendExpectStringArray(string cmd, params object[] args)
		{
			byte[][] reply = SendExpectDataArray(cmd, args);
			var keys = new string[reply.Length];
			for (int i = 0; i < reply.Length; i++)
				keys[i] = Encoding.UTF8.GetString(reply[i]);
			return keys;
		}

		public byte[][] SendExpectDataArray(string cmd, params object[] args)
		{
			if (!SendCommand(cmd, args))
				throw new Exception("Unable to connect");
			int c = bstream.ReadByte();
			if (c == -1)
				throw new ResponseException("No more data");

			string s = ReadLine();
			Log("S", (char)c + s);
			if (c == '-')
				throw new ResponseException(s.StartsWith("ERR ") ? s.Substring(4) : s);
			if (c == '*')
			{
				int count;
				if (int.TryParse(s, out count))
				{
					var result = new byte[count][];

					for (int i = 0; i < count; i++)
						result[i] = ReadData();

					return result;
				}
			}
			throw new ResponseException("Unknown reply on multi-request: " + c + s);
		}

		#region 列表 操作 命令

		/// <summary>
		/// LRANGE key start stop 返回列表 key 中指定区间内的元素，区间以偏移量 start 和 stop 指定。
		/// 下标(index)参数 start 和 stop 都以 0 为底，也就是说，以 0 表示列表的第一个元素，以 1 表示列表的第二个元素，以此类推。
		/// 你也可以使用负数下标，以 -1 表示列表的最后一个元素， -2 表示列表的倒数第二个元素，以此类推。
		/// 注意LRANGE命令和编程语言区间函数的区别
		/// 假如你有一个包含一百个元素的列表，对该列表执行 LRANGE list 0 10 ，结果是一个包含11个元素的列表，
		/// 这表明 stop 下标也在 LRANGE 命令的取值范围之内(闭区间)，这和某些语言的区间函数可能不一致，比如Ruby的 Range.new 、 Array#slice 和Python的 range() 函数。
		/// 超出范围的下标值不会引起错误。
		/// 如果 start 下标比列表的最大下标 end ( LLEN list 减去 1 )还要大，那么 LRANGE 返回一个空列表。
		/// 如果 stop 下标比 end 下标还要大，Redis将 stop 的值设置为 end 。
		/// </summary>
		/// <param name="key"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <returns></returns>
		public byte[][] ListRange(string key, int start, int end)
		{
			return SendExpectDataArray("LRANGE", key, start, end);
		}

		public void LeftPush(string key, string value)
		{
			LeftPush(key, Encoding.UTF8.GetBytes(value));
		}

		/// <summary>
		/// LPUSH key value [value ...]  将一个或多个值 value 插入到列表 key 的表头
		/// 如果有多个 value 值，那么各个 value 值按从左到右的顺序依次插入到表头： 
		/// 比如说，对空列表 mylist 执行命令 LPUSH mylist a b c ，列表的值将是 c b a ，
		/// 这等同于原子性地执行 LPUSH mylist a 、 LPUSH mylist b 和 LPUSH mylist c 三个命令。
		/// 如果 key 不存在，一个空列表会被创建并执行 LPUSH 操作。
		/// 当 key 存在但不是列表类型时，返回一个错误。
		/// 在Redis 2.4版本以前的 LPUSH 命令，都只接受单个 value 值。 
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void LeftPush(string key, byte[] value)
		{
			SendDataCommand(value, "LPUSH", key);
			ExpectSuccess();
		}

		public void RightPush(string key, string value)
		{
			RightPush(key, Encoding.UTF8.GetBytes(value));
		}

		/// <summary>
		/// RPUSH key value [value ...]	 将一个或多个值 value 插入到列表 key 的表尾(最右边)。
		/// 如果有多个 value 值，那么各个 value 值按从左到右的顺序依次插入到表尾：
		/// 比如对一个空列表 mylist 执行 RPUSH mylist a b c ，得出的结果列表为 a b /c ，
		/// 等同于执行命令 RPUSH mylist a 、 RPUSH mylist b 、 RPUSH mylist c 。
		/// 如果 key 不存在，一个空列表会被创建并执行 RPUSH 操作。
		/// 当 key 存在但不是列表类型时，返回一个错误。
		/// 在 Redis 2.4 版本以前的 RPUSH 命令，都只接受单个 value 值。
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void RightPush(string key, byte[] value)
		{
			SendDataCommand(value, "RPUSH", key);
			ExpectSuccess();
		}

		/// <summary>
		/// LLEN key 返回列表 key 的长度。
		/// 如果 key 不存在，则 key 被解释为一个空列表，返回 0 .
		/// 如果 key 不是列表类型，返回一个错误。
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>	
		public int ListLength(string key)
		{
			return SendExpectInt("LLEN", key);
		}



		/// <summary>
		/// LINDEX key index  返回列表 key 中，下标为 index 的元素。
		/// 下标(index)参数 start 和 stop 都以 0 为底，也就是说，以 0 表示列表的第一个元素，以 1 表示列表的第二个元素，以此类推。
		/// 你也可以使用负数下标，以 -1 表示列表的最后一个元素， -2 表示列表的倒数第二个元素，以此类推。
		/// 如果 key 不是列表类型，返回一个错误。
		/// </summary>
		/// <param name="key"></param>
		/// <param name="index"></param>
		/// <returns></returns>	
		public byte[] ListIndex(string key, int index)
		{
			SendCommand("LINDEX", key, index);
			return ReadData();
		}

		/// <summary>
		/// LPOP key
		/// 移除并返回列表 key 的头元素。
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public byte[] LeftPop(string key)
		{
			SendCommand("LPOP", key);
			return ReadData();
		}

		/// <summary>
		/// RPOP key
		/// 移除并返回列表 key 的尾元素。
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public byte[] RightPop(string key)
		{
			SendCommand("RPOP", key);
			return ReadData();
		}

		#endregion

		#region Set commands

		/// <summary>
		/// SADD key member [member ...]  将一个或多个 member 元素加入到集合 key 当中，已经存在于集合的 member 元素将被忽略。
		/// 假如 key 不存在，则创建一个只包含 member 元素作成员的集合。
		/// 当 key 不是集合类型时，返回一个错误。
		/// 在Redis2.4版本以前， SADD 只接受单个 member 值。
		/// </summary>
		/// <param name="key"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		public bool AddToSet(string key, byte[] member)
		{
			return SendDataExpectInt(member, "SADD", key) > 0;
		}

		public bool AddToSet(string key, string member)
		{
			return AddToSet(key, Encoding.UTF8.GetBytes(member));
		}

		/// <summary>
		/// SCARD key 返回集合 key 的基数(集合中元素的数量)。
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public int CardinalityOfSet(string key)
		{
			return SendExpectInt("SCARD", key);
		}

		/// <summary>
		/// SISMEMBER key member  判断 member 元素是否集合 key 的成员。
		/// </summary>
		/// <param name="key"></param>
		/// <param name="member"></param>
		/// <returns></returns>		
		public bool IsMemberOfSet(string key, byte[] member)
		{
			return SendDataExpectInt(member, "SISMEMBER", key) > 0;
		}

		public bool IsMemberOfSet(string key, string member)
		{
			return IsMemberOfSet(key, Encoding.UTF8.GetBytes(member));
		}

		/// <summary>
		/// SMEMBERS key 返回集合 key 中的所有成员。
		/// 不存在的 key 被视为空集合。
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public byte[][] GetMembersOfSet(string key)
		{
			return SendExpectDataArray("SMEMBERS", key);
		}

		/// <summary>
		/// SRANDMEMBER key [count] 如果命令执行时，只提供了 key 参数，那么返回集合中的一个随机元素。
		/// 从 Redis 2.6 版本开始， SRANDMEMBER 命令接受可选的 count 参数：
		/// 如果 count 为正数，且小于集合基数，那么命令返回一个包含 count 个元素的数组，数组中的元素各不相同。
		/// 如果 count 大于等于集合基数，那么返回整个集合。
		/// 如果 count 为负数，那么命令返回一个数组，数组中的元素可能会重复出现多次，而数组的长度为 count 的绝对值。
		/// 该操作和 SPOP 相似，但 SPOP 将随机元素从集合中移除并返回，而 SRANDMEMBER 则仅仅返回随机元素，而不对集合进行任何改动。
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>	
		public byte[] GetRandomMemberOfSet(string key)
		{
			return SendExpectData("SRANDMEMBER", key);
		}

		/// <summary>
		/// SPOP key 移除并返回集合中的一个随机元素。
		/// 如果只想获取一个随机元素，但不想该元素从集合中被移除的话，可以使用 SRANDMEMBER 命令。
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public byte[] PopRandomMemberOfSet(string key)
		{
			return SendExpectData("SPOP", key);
		}

		/// <summary>
		/// SREM key member [member ...]  移除集合 key 中的一个或多个 member 元素，不存在的 member 元素会被忽略。
		/// 当 key 不是集合类型，返回一个错误。
		/// 在 Redis 2.4 版本以前， SREM 只接受单个 member 值。
		/// </summary>
		/// <param name="key"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		public bool RemoveFromSet(string key, byte[] member)
		{
			return SendDataExpectInt(member, "SREM", key) > 0;
		}

		public bool RemoveFromSet(string key, string member)
		{
			return RemoveFromSet(key, Encoding.UTF8.GetBytes(member));
		}

		/// <summary>
		/// SUNION key [key ...]  返回一个集合的全部成员，该集合是所有给定集合的并集。
		/// 不存在的 key 被视为空集。
		/// </summary>
		/// <param name="keys"></param>
		/// <returns></returns>
		public byte[][] GetUnionOfSets(params string[] keys)
		{
			if (keys == null)
				throw new ArgumentNullException();

			return SendExpectDataArray("SUNION", keys);
		}

		private void StoreSetCommands(string cmd, params string[] keys)
		{
			if (String.IsNullOrEmpty(cmd))
				throw new ArgumentNullException("cmd");

			if (keys == null)
				throw new ArgumentNullException("keys");

			SendExpectSuccess(cmd, keys);
		}

		/// <summary>
		/// SUNIONSTORE destination key [key ...]
		/// 这个命令类似于 SUNION 命令，但它将结果保存到 destination 集合，而不是简单地返回结果集。
		/// 如果 destination 已经存在，则将其覆盖。
		/// destination 可以是 key 本身。
		/// </summary>
		/// <param name="keys"></param>
		public void StoreUnionOfSets(params string[] keys)
		{
			StoreSetCommands("SUNIONSTORE", keys);
		}

		/// <summary>
		/// SINTER key [key ...] 返回一个集合的全部成员，该集合是所有给定集合的交集。
		/// 不存在的 key 被视为空集。
		/// 当给定集合当中有一个空集时，结果也为空集(根据集合运算定律)。
		/// </summary>
		/// <param name="keys"></param>
		/// <returns></returns>
		public byte[][] GetIntersectionOfSets(params string[] keys)
		{
			if (keys == null)
				throw new ArgumentNullException();

			return SendExpectDataArray("SINTER", keys);
		}

		public void StoreIntersectionOfSets(params string[] keys)
		{
			StoreSetCommands("SINTERSTORE", keys);
		}

		/// <summary>
		/// SDIFF key [key ...]  返回一个集合的全部成员，该集合是所有给定集合之间的差集。
		/// 不存在的 key 被视为空集。
		/// </summary>
		/// <param name="keys"></param>
		/// <returns></returns>
		public byte[][] GetDifferenceOfSets(params string[] keys)
		{
			if (keys == null)
				throw new ArgumentNullException();

			return SendExpectDataArray("SDIFF", keys);
		}

		/// <summary>
		/// SDIFFSTORE destination key [key ...]
		/// 这个命令的作用和 SDIFF 类似，但它将结果保存到 destination 集合，而不是简单地返回结果集。
		/// 如果 destination 集合已经存在，则将其覆盖。
		/// destination 可以是 key 本身。
		/// </summary>
		/// <param name="keys"></param>
		public void StoreDifferenceOfSets(params string[] keys)
		{
			StoreSetCommands("SDIFFSTORE", keys);
		}

		/// <summary>
		/// SMOVE source destination member	将 member 元素从 source 集合移动到 destination 集合。
		/// SMOVE 是原子性操作。
		/// 如果 source 集合不存在或不包含指定的 member 元素，则 SMOVE 命令不执行任何操作，仅返回 0 。
		/// 否则， member 元素从 source 集合中被移除，并添加到 destination 集合中去。
		/// 当 destination 集合已经包含 member 元素时， SMOVE 命令只是简单地将 source 集合中的 member 元素删除。
		/// 当 source 或 destination 不是集合类型时，返回一个错误。
		/// </summary>
		/// <param name="srcKey"></param>
		/// <param name="destKey"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		public bool MoveMemberToSet(string srcKey, string destKey, byte[] member)
		{
			return SendDataExpectInt(member, "SMOVE", srcKey, destKey) > 0;
		}

		#endregion

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~Redis()
		{
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{

				//QUIT
				//请求服务器关闭与当前客户端的连接。
				//一旦所有等待中的回复(如果有的话)顺利写入到客户端，连接就会被关闭。
				SendCommand("QUIT");
				ExpectSuccess();
				socket.Close();
				socket = null;
			}
		}
	}

	public class SortOptions
	{
		public string Key { get; set; }
		public bool Descending { get; set; }
		public bool Lexographically { get; set; }
		public Int32 LowerLimit { get; set; }
		public Int32 UpperLimit { get; set; }
		public string By { get; set; }
		public string StoreInKey { get; set; }
		public string Get { get; set; }

		public object[] ToArgs()
		{
			var args = new ArrayList();
			if (LowerLimit != 0 || UpperLimit != 0)
			{
				args.Add("LIMIT");
				args.Add(LowerLimit);
				args.Add(UpperLimit);
			}
			if (Lexographically)
				args.Add("ALPHA");
			if (!string.IsNullOrEmpty(By))
			{
				args.Add("BY");
				args.Add(By);
			}
			if (!string.IsNullOrEmpty(Get))
			{
				args.Add("GET");
				args.Add(Get);
			}
			return args.ToArray();
		}
	}
}
