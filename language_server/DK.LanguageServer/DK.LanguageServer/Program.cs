using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using DK.Common;

namespace DK.LanguageServer
{
	class Program
	{
		static void Main(string[] args)
		{
			try
			{
				Log.Initialize(AppDataDir, Properties.Resources.LogFileNameFormat);

				Environment.ExitCode = new Program().Run();

				Log.Info("Shutting down normally.");
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Fatal Exception");
				Environment.ExitCode = 1;
			}
			finally
			{
				Log.Shutdown();
			}
		}

		private int Run()
		{
			Log.Info("Language server starting...");

			var server = new LanguageServer(Console.OpenStandardOutput(), Console.OpenStandardInput());
			bool running = true;
			server.Disconnected += (sender, e) =>
			{
				Log.Info("Disconnect detected.");
				running = false;
			};

			while (running)
			{
				System.Threading.Thread.Sleep(1000);
			}

			Log.Info("Language server shutting down");
			return 0;
		}

		private static string _appDataDir;
		public static string AppDataDir
		{
			get
			{
				if (_appDataDir == null)
				{
					_appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
						Properties.Resources.AppDataDirName);
					if (!Directory.Exists(_appDataDir)) Directory.CreateDirectory(_appDataDir);
				}
				return _appDataDir;
			}
		}
	}
}
