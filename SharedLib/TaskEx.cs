using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
		var thread = new Thread(() => taskToRun.WaitAndThrow(onExceptionThrown));
		thread.Start();
		return thread;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="taskToRun"></param>
	/// <param name="onExceptionThrown"></param>
	private static void WaitAndThrow(this Task taskToRun, Action<AggregateException>? onExceptionThrown = null)
	{
		try
		{
			taskToRun.Wait();
		}
		catch (AggregateException e)
		{
			if (onExceptionThrown != null)
				onExceptionThrown.Invoke(e);
			else
				Console.WriteLine(e);
		}
	}

	/// <summary>
	/// Pauses execution until all tasks are complete.
	/// </summary>
	/// <param name="tasks"></param>
	/// <exception cref="AggregateException"></exception>
	public static void WaitForAll(this List<Task> tasks)
	{
		Exception? exception = null;
		
		foreach (var task in tasks)
			task.WaitAndThrow(e => exception = e);
		
		if (exception is not null)
			throw exception;
	}
}