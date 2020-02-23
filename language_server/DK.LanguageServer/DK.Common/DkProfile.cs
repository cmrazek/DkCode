using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace DK.Common
{
	/// <summary>
	/// Stores settings and cached files for a WBDK ACM profile.
	/// </summary>
	public class DkProfile
	{
		private string _appName;
		private string _wbdkKeyPath = null;

		private string[] _sourcePaths;
		private string[] _includePaths;
		private string[] _executablePaths;

		private const string WbdkRegKey64 = @"SOFTWARE\WOW6432Node\Fincentric\WBDK";
		private const string WbdkRegKey32 = @"SOFTWARE\Fincentric\WBDK";

		public DkProfile(string appName = null)
		{
			_appName = appName;
			if (string.IsNullOrEmpty(_appName)) _appName = GetDefaultProfileName();
			LoadProfile();
		}

		private void ClearProfile()
		{
			_sourcePaths = new string[0];
			_includePaths = new string[0];
			_executablePaths = new string[0];
		}

		private void LoadProfile()
		{
			ClearProfile();

			using (var rootKey = FindWbdkKey())
			{
				if (rootKey != null)
				{
					using (var key = rootKey.OpenSubKey($"Configurations\\{_appName}"))
					{
						if (key != null)
						{
							var rootPath = GetRegString(key, "RootPath").Trim();
							_sourcePaths = GetRegPaths(key, "SourcePaths", rootPath);
							_includePaths = GetRegPaths(key, "IncludePaths", rootPath);
							_executablePaths = GetRegPaths(key, "ExecutablePaths", rootPath);
						}
					}
				}
			}
		}

		private string GetDefaultProfileName()
		{
			using (var key = FindWbdkKey())
			{
				if (key != null)
				{
					return GetRegString(key, "CurrentConfig");
				}
			}

			return null;
		}

		private RegistryKey FindWbdkKey()
		{
			if (_wbdkKeyPath != null)
			{
				return Registry.LocalMachine.OpenSubKey(_wbdkKeyPath, false);
			}
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return null;

			var key = Registry.LocalMachine.OpenSubKey(WbdkRegKey64, false);
			if (key != null) return key;

			key = Registry.LocalMachine.OpenSubKey(WbdkRegKey32, false);
			if (key != null) return key;

			return null;
		}

		private string GetRegString(RegistryKey key, string valueName, string defaultValue = "")
		{
			if (key == null) return defaultValue;

			var val = key.GetValue(valueName);
			if (val == null) return defaultValue;
			return Convert.ToString(val);
		}

		private string GetRegPath(RegistryKey key, string valueName, string rootPath)
		{
			var path = GetRegString(key, valueName);
			return CombineRootPath(rootPath, path);
		}

		private string[] GetRegPaths(RegistryKey key, string valueName, string rootPath)
		{
			return GetRegString(key, valueName).Split(';')
				.Where(x => !string.IsNullOrEmpty(x))
				.Select(x => CombineRootPath(rootPath, x.Trim()))
				.ToArray();
		}

		private string CombineRootPath(string rootPath, string path)
		{
			if (string.IsNullOrEmpty(rootPath)) return path;
			if (string.IsNullOrEmpty(path)) return rootPath;

			if (rootPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				if (path.StartsWith(Path.DirectorySeparatorChar.ToString()))
				{
					return string.Concat(rootPath, path.Substring(1));
				}
				else
				{
					return string.Concat(rootPath, path);
				}
			}
			else
			{
				if (path.StartsWith(Path.DirectorySeparatorChar.ToString()))
				{
					return string.Concat(rootPath, path);
				}
				else
				{
					return string.Concat(rootPath, Path.DirectorySeparatorChar, path);
				}
			}
		}

		public Document GetDocument(Uri uri)
		{
			var fileName = uri.LocalPath;
			if (!File.Exists(fileName)) throw new DocumentNotFoundException(uri);
			var content = File.ReadAllText(fileName);
			return new Document(uri, content, 0);
		}

		public Document TryGetIncludeFile(string relativeFileName, Document fromDocument, bool includeSystemPaths)
		{
			var fromDir = Path.GetDirectoryName(fromDocument.Uri.LocalPath);
			var fileName = Path.GetFullPath(Path.Combine(fromDir, relativeFileName));

			// Search for an immediate neighbor
			if (File.Exists(fileName))
			{
				return new Document(new Uri(fileName), File.ReadAllText(fileName), 0);
			}

			// Search the include paths
			foreach (var includeDir in _includePaths)
			{
				fileName = Path.GetFullPath(Path.Combine(includeDir, relativeFileName));
				if (File.Exists(fileName))
				{
					return new Document(new Uri(fileName), File.ReadAllText(fileName), 0);
				}
			}

			if (includeSystemPaths)
			{
				// TODO
			}

			return null;
		}
	}
}
