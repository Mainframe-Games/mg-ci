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
		var thread = new Thread(() => { taskToRun.WaitAndThrow(onExceptionThrown); });
		thread.Start();
		return thread;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="taskToRun"></param>
	/// <param name="onExceptionThrown"></param>
	public static void WaitAndThrow(this Task taskToRun, Action<AggregateException>? onExceptionThrown = null)
	{
		try
		{
			taskToRun.Wait();
		}
		catch (AggregateException e)
		{
			Console.WriteLine(e);
			onExceptionThrown?.Invoke(e);
		}
	}

	/// <summary>
	/// Pauses execution until all tasks are complete.
	/// </summary>
	/// <param name="tasks"></param>
	/// <exception cref="AggregateException"></exception>
	public static void WaitForAll(this IEnumerable<Task> tasks)
	{
		Exception? exception = null;
		
		foreach (var task in tasks)
			task.WaitAndThrow(e => exception = e);
		
		if (exception is not null)
			throw exception;
	}
}