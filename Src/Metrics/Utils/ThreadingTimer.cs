using System;

namespace Metrics.Utils
{
    /// <summary>
    /// Represents the method that will handle an event that has no event data.
    /// </summary>
    /// <remarks>Used to convert the Timer callback into an event.</remarks>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">An object that contains no event data.</param>
    public delegate void TimerEventHandler(object sender, EventArgs e);

    /// <summary>
    /// Interface used to wrap <c>System.Threading.Timer</c>.
    /// </summary>
    public interface ITimer : IDisposable
    {
        /// <summary>
        /// Represents the method that will handle an event.
        /// </summary>
        event TimerEventHandler Tick;

        /// <summary>
        /// Changes the start time and the interval between method invocations for a timer.
        /// </summary>
        /// <param name="dueTime">The amount of time to delay before the callback parameter invokes its methods.</param>
        /// <param name="period">The time interval between invocations of the methods referenced by callback</param>
        void Change(TimeSpan dueTime, TimeSpan period);
    }

    /// <summary>
    /// Wrapper for <c>System.Threading.Timer</c>.
    /// </summary>
    public class ThreadingTimer : ITimer
    {
        private readonly System.Threading.Timer timer;

        /// <summary>
        /// Changes the start time and the interval between method invocations for a timer.
        /// </summary>
        /// <param name="state">An object containing information to be used by the callback method, or null.</param>
        /// <param name="dueTime">The amount of time to delay before the callback parameter invokes its methods.</param>
        /// <param name="period">The time interval between invocations of the methods referenced by callback</param>
        public ThreadingTimer(object state, TimeSpan dueTime, TimeSpan period)
        {
            timer = new System.Threading.Timer(TimerCallback, state, dueTime, period);
        }

        /// <summary>
        /// Represents the method that will handle the TimerCallback event.
        /// </summary>
        public event TimerEventHandler Tick;

        /// <summary>
        /// Changes the start time and the interval between method invocations for a timer.
        /// </summary>
        /// <param name="dueTime">The amount of time to delay before the callback parameter invokes its methods.</param>
        /// <param name="period">The time interval between invocations of the methods referenced by callback</param>
        public void Change(TimeSpan dueTime, TimeSpan period)
        {
            timer.Change(dueTime, period);
        }

        /// <summary>
        /// Releases all resources used by the current instance of Timer.
        /// </summary>
        public void Dispose()
        {
            timer.Dispose();
        }

        private void TimerCallback(object state)
        {
            Tick?.Invoke(this, new EventArgs());
        }
    }
}
