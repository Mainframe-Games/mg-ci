using SharedLib;

namespace Deployment.Misc;

public static class TaskEx
{
	/// <summary>
	/// Runs task on a new thread.
	/// Within lambda waits for completion logging any exceptions
	/// </summary>
	/// <param name="taskToRun"></param>
	/// <param name="onExceptionThrown"></param>
	public static void FireAndForget(this Task taskToRun, Action<Exception>? onExceptionThrown = null)
	{
		new Thread(() =>
		{
			try
			{
				var task = Task.Run(() => taskToRun);
				task.Wait();
			}
			catch (AggregateException ae)
			{
				foreach (var e in ae.InnerExceptions)
				{
					Logger.Log(e);
					onExceptionThrown?.Invoke(e);
				}
			}
		}).Start();
	}
}