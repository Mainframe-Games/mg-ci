namespace SharedLib;

public static class TaskEx
{
	/// <summary>
	/// Runs task on a new thread.
	/// Within lambda waits for completion logging any exceptions
	/// </summary>
	/// <param name="taskToRun"></param>
	/// <param name="onExceptionThrown"></param>
	public static Thread FireAndForget(this Task taskToRun, Action<Exception>? onExceptionThrown = null)
	{
		var thread = new Thread(() =>
		{
			try
			{
				taskToRun.Wait();
			}
			catch (AggregateException ae)
			{
				foreach (var e in ae.InnerExceptions)
				{
					Console.WriteLine(e);
					onExceptionThrown?.Invoke(e);
				}
			}
		});
		thread.Start();
		return thread;
	}
}