using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace DK.LanguageServer
{
	class Program
	{
		//private static string _pipeName;

		static void Main(string[] args)
		{
			try
			{
				//if (args.Length != 1) throw new ArgumentException("Expected 1 argument: name of named pipe");
				//_pipeName = args[0];
				//if (string.IsNullOrWhiteSpace(_pipeName)) throw new ArgumentException("Invalid pipe name");

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
				Environment.ExitCode = new Program().RunAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

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

		private async Task<int> RunAsync()
		{
			Log.Info("Language server starting...");
			//Log.Info("Pipe Name: '{0}'", _pipeName);

			
			// TODO: remove
			//using (var stream = new System.IO.Pipes.NamedPipeServerStream(_pipeName, PipeDirection.InOut,
			//		NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte,
			//		PipeOptions.Asynchronous))
			//{
			//	Log.Debug("Waiting for connection...");
			//	await stream.WaitForConnectionAsync();

			Log.Debug("Got connection; starting LanguageServer()");
			var server = new LanguageServer(Console.OpenStandardOutput(), Console.OpenStandardInput());
			bool running = true;
			server.Disconnected += (sender, e) =>
			{
				Log.Info("Disconnect detected.");
				running = false;
			};

			while (running)
			{
				await Task.Delay(1000);
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
