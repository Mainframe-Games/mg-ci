using System;
using UnityEngine;

namespace BuildSystem.Utils
{
	public static class BS_Logger
	{
		public static void Log(object log, LogType logType = LogType.Log)
		{
			if (Application.isBatchMode)
			{
				Console.WriteLine(log);
				return;
			}

			switch (logType)
			{
				case LogType.Error:
					Debug.LogError(log);
					break;
				case LogType.Assert:
					Debug.LogAssertion(log);
					break;
				case LogType.Warning:
					Debug.LogWarning(log);
					break;
				case LogType.Log:
					Debug.Log(log);
					break;
				case LogType.Exception:
					Debug.LogException(log as Exception);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(logType), logType, null);
			}
		}
	}
}