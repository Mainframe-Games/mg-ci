using System.Timers;
using Timer = System.Timers.Timer;

namespace SharedLib;

public class TimeEvent
{
	private readonly int _hour;
	private readonly int _minute;
	private readonly bool _weekdaysOnly;

	public Action OnEventTriggered;

	private readonly Timer _timer;

	public TimeEvent(int hour, int minute, bool weekDaysOnly = true)
	{
		_hour = hour;
		_minute = minute;
		_weekdaysOnly = weekDaysOnly;

		var initialDelay = GetDelayMs();
		_timer = new Timer(initialDelay);
		_timer.AutoReset = true;
		_timer.Elapsed += TimerElapsed;
		_timer.Start();

		Logger.Log($"Event scheduled to run event at {hour}:{minute} every {(weekDaysOnly ? "weekday" : "day")}");
	}

	private double GetDelayMs()
	{
		var now = DateTime.Now;
		var targetTime = new DateTime(now.Year, now.Month, now.Day, _hour, _minute, 0);
		
		if (now > targetTime)
			targetTime = targetTime.AddDays(1);

		var delay = (targetTime - now).TotalMilliseconds;
		return delay;
	}

	private void TimerElapsed(object sender, ElapsedEventArgs e)
	{
		var now = DateTime.Now;

		// check weekdays
		if (_weekdaysOnly && now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
			return;

		// Check if it's time
		if (now.Hour != _hour || now.Minute != _minute)
			return;

		// Trigger your event here
		Console.WriteLine($"Event invoked at {_hour}:{_minute}");
		OnEventTriggered.Invoke();
        
		// reset timer interval for next day
		_timer.Interval = GetDelayMs();
	}

	public void Stop()
	{
		_timer.Stop();
	}
}