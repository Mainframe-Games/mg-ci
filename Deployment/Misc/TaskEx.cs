using SharedLib;

namespace Deployment.Misc;

public static class TaskEx
{
	/// <summary>
	/// Uses Task.Run() and waits for completion logging any exceptions
	/// </summary>
	/// <param name="taskToRun"></param>
	/// <param name="onExceptionThrown"></param>
	public static void FireAndForget(this Task taskToRun, Action<Exception>? onExceptionThrown = null)
	{
		var task = Task.Run(() => taskToRun);
		
		try
		{
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
	}
}