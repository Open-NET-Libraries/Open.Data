using Open.Threading;
using System;
using System.Data;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;


namespace Open.Serialization
{
	public static class Extensions
	{
		private static void SerializeToInternal<T>(string path, T data, FileMode filemode = FileMode.OpenOrCreate)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Cannot be whitespace or empty.", nameof(path));
			Contract.EndContractBlock();

			if (data == null)
				return;

			if (path.EndsWith(".xml", StringComparison.CurrentCultureIgnoreCase))
			{

				if (data is DataTable table)
				{

					if (table.TableName == null)
						table.TableName = path;

					if (table.DataSet == null)
						(new DataSet()).Tables.Add(table);

					table.WriteXml(path, XmlWriteMode.WriteSchema);
					return;
				}

				if (data is DataSet dataset)
				{
					dataset.WriteXml(path);
					return;
				}

				var formatter = new XmlSerializer(typeof(T));
				using (var fs = new FileStream(path,
					filemode, FileAccess.Write, FileShare.None))
					formatter.Serialize(fs, data);

			}
			else
			{

				var formatter = new BinaryFormatter();
				using (var fs = new FileStream(path,
					filemode, FileAccess.Write, FileShare.None))
					formatter.Serialize(fs, data);
			}
		}

		/// <summary>
		/// Stores the data to disk at the specified location path.  Infers XML serialization if the file extension is XML.  Otherwise binary.
		/// </summary>
		/// <typeparam name="T">The data type.</typeparam>
		/// <param name="path">The location to save to.</param>
		/// <param name="data"></param>
		public static void SerializeTo<T>(string path, T data)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Cannot be whitespace or empty.", nameof(path));
			Contract.EndContractBlock();

			ThreadSafety.File.EnsureDirectory(path);
			ThreadSafety.File.WriteTo(path, () => SerializeToInternal(path, data));
		}

		private static T DeserializeFromInternal<T>(string path, out DateTime? modified)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Cannot be whitespace or empty.", nameof(path));
			Contract.EndContractBlock();

			if (!File.Exists(path))
			{
				modified = null;
				return default;
			}

			modified = File.GetLastWriteTime(path);
			using (FileStream fs = ThreadSafety.File.Unsafe.GetFileStreamForRead(path))
			{
				if (fs.Length == 0)
					return default;

				if (path.EndsWith(".xml", StringComparison.CurrentCultureIgnoreCase))
				{
					var type = typeof(T);

					if (type == typeof(DataTable))
					{
						var table = new DataTable();
						table.ReadXml(path);
						return (T)((object)table);
					}

					if (type == typeof(DataSet))
					{
						var dataset = new DataSet();
						dataset.ReadXml(path);
						return (T)(object)dataset;
					}

					var formatter = new XmlSerializer(typeof(T));
					var result = formatter.Deserialize(fs);
					return result == null ? default : (T)result;
				}
				else
				{
					var formatter = new BinaryFormatter();
					return (T)formatter.Deserialize(fs);
				}
			}
		}

		public static T DeserializeFrom<T>(string path)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Cannot be whitespace or empty.", nameof(path));
			Contract.EndContractBlock();

			return DeserializeFrom<T>(path, out _);
		}

		public static T DeserializeFrom<T>(string path, out DateTime? modified)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Cannot be whitespace or empty.", nameof(path));
			Contract.EndContractBlock();

			if (!File.Exists(path))
			{
				modified = null;
				return default;
			}

			DateTime? writetime = null;
			T result = ThreadSafety.File.ReadFrom(path, () =>
			{
				var r = DeserializeFromInternal<T>(path, out DateTime? wt);
				writetime = wt;
				return r;
			});

			modified = writetime;
			return result;
		}

		public static T DeserializeExisting<T>(string path, out DateTime? modified, Func<T> dataFactory)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Cannot be whitespace or empty.", nameof(path));
			Contract.EndContractBlock();

			T result = DeserializeFrom<T>(path, out DateTime? writetime);
			if (!writetime.HasValue)
			{
				if (!ThreadSafety.File.WriteToIfNotExists(path, () =>
				{
					result = dataFactory();
					SerializeToInternal(path, result);
					writetime = DateTime.Now;
				}))
				{
					result = DeserializeFrom<T>(path, out writetime);
					if (!writetime.HasValue)
						throw new Exception("Unable to retrive file for deserialization: " + path);
				}
			}

			modified = writetime;
			return result;
		}

	}
}
