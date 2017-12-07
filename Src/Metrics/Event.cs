
using System;
using System.Collections.Generic;

namespace Metrics
{
    public interface Event : ResetableMetric
    {
        void Record();

        void Record(DateTime timestamp);

        void Record(Dictionary<string, object> fields);

        void Record(Dictionary<string, object> fields, DateTime timestamp);
    }
}
