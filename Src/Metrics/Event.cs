
using System;
using System.Collections.Generic;

namespace Metrics
{
    /// <summary>
    /// An event is used to record the occurance of something taking place.
    /// It stores the recored data internally until the data is reported.
    /// </summary>
    public interface Event : ResetableMetric
    {
        /// <summary>
        /// Record an event. Uses a current utc timestamp at the time of recording and a default timestamp field.
        /// </summary>
        void Record();

        /// <summary>
        /// Record an event. Uses the specified timestamp and a default timestamp field.
        /// </summary>
        /// <param name="timestamp">The timestamp of the event.</param>
        void Record(DateTime timestamp);

        /// <summary>
        /// Record an event. Uses a current utc timestamp at the time of recording and the specified fields.
        /// </summary>
        /// <param name="fields">The fields associated with the event.</param>
        void Record(Dictionary<string, object> fields);

        /// <summary>
        /// Record an event. Uses the specified timestamp and fields.
        /// </summary>
        /// <param name="fields">The fields associated with the event.</param>
        /// <param name="timestamp">The timestamp of the event.</param>
        void Record(Dictionary<string, object> fields, DateTime timestamp);

        /// <summary>
        /// Removes the specified total number of items from the start of the event list.
        /// </summary>
        /// <param name="count">The total number of events to remove.</param>
        void RemoveRangeFromStartIndex(int count);

    }
}
