using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using DK.LanguageServer.Jobs;

namespace DK.LanguageServer
{
	class WorkThread
	{
		private LanguageServer _server;
		private Thread _thread;
		private ManualResetEvent _kill = new ManualResetEvent(false);
		private ConcurrentQueue<BaseJob> _jobs = new ConcurrentQueue<BaseJob>();

		public WorkThread(LanguageServer languageServer)
		{
			_server = languageServer ?? throw new ArgumentNullException(nameof(languageServer));
		}

		public void Start()
		{
			if (_thread != null) throw new InvalidOperationException("Thread has already been started.");
			_thread = new Thread(new ThreadStart(ThreadProc));
			_thread.Name = "Analysis Thread";
			_thread.Priority = ThreadPriority.Normal;
			_thread.IsBackground = true;
			_thread.Start();
		}

		public void Kill()
		{
			if (_thread != null && _thread.IsAlive)
			{
				_kill.Set();
				_thread.Join();
				_thread = null;
			}
		}

		private void ThreadProc()
		{
			try
			{
				while (!_kill.WaitOne(Const.AnalysisThreadIdleMilliseconds))
				{
					if (_jobs.TryDequeue(out var job))
					{
						try
						{
							job.Execute();
						}
						catch (Exception ex)
						{
							Log.Error(ex, "Exception when executing job: {0}", job);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Fatal exception in analysis thread.");
			}
		}

		public void Enqueue(BaseJob job)
		{
			_jobs.Enqueue(job);
		}
	}
}
