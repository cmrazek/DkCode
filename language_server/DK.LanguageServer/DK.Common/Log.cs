using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DK.Common
{
	public static class Log
	{
		private static string _directory;
		private static string _fileNamePattern;
		private static StreamWriter _file;

		enum LogLevel
		{
			Debug,
			Info,
			Warning,
			Error
		}

		public static void Initialize(string directory, string fileNamePattern)
		{
			_directory = directory;
			_fileNamePattern = fileNamePattern;
		}

		public static void Shutdown()
		{
			if (_file != null)
			{
				_file.Close();
				_file = null;
			}
		}

		private static void Write(LogLevel level, string message)
		{
			var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ({level}) {message}";
			System.Diagnostics.Debug.WriteLine(line);

			if (CheckFile())
			{
				_file.WriteLine(line);
			}
		}

		private static bool CheckFile()
		{
			try
			{
				if (_file != null) return true;
				if (string.IsNullOrEmpty(_directory) || string.IsNullOrEmpty(_fileNamePattern)) return false;

				_file = new StreamWriter(Path.Combine(_directory, string.Format(_fileNamePattern, DateTime.Now)));
				return true;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Exception when creating log file: {ex}");
				_file = null;
				return false;
			}
		}

		public static void Debug(string message)
		{
			Write(LogLevel.Debug, message);
		}

		public static void Debug(string format, params object[] args)
		{
			Write(LogLevel.Debug, string.Format(format, args));
		}

		public static void Info(string message)
		{
			Write(LogLevel.Info, message);
		}

		public static void Info(string format, params object[] args)
		{
			Write(LogLevel.Info, string.Format(format, args));
		}

		public static void Warning(string message)
		{
			Write(LogLevel.Warning, message);
		}

		public static void Warning(string format, params object[] args)
		{
			Write(LogLevel.Warning, string.Format(format, args));
		}

		public static void Warning(Exception ex, string message)
		{
			Write(LogLevel.Warning, string.Concat(message, "\r\n", ex));
		}

		public static void Warning(Exception ex, string format, params object[] args)
		{
			Write(LogLevel.Warning, string.Concat(string.Format(format, args), "\r\n", ex));
		}

		public static void Error(string message)
		{
			Write(LogLevel.Error, message);
		}

		public static void Error(string format, params object[] args)
		{
			Write(LogLevel.Error, string.Format(format, args));
		}

		public static void Error(Exception ex, string message)
		{
			Write(LogLevel.Error, string.Concat(message, "\r\n", ex));
		}

		public static void Error(Exception ex, string format, params object[] args)
		{
			Write(LogLevel.Error, string.Concat(string.Format(format, args), "\r\n", ex));
		}
	}
}
