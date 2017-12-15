
using System.Collections.Generic;
using System.Linq;
using Metrics.MetricData;

namespace Metrics.Json
{
    public class JsonEvent : JsonMetric
    {
        public List<EventDetails> Events = new List<EventDetails>();

        public static JsonEvent FromEvent(MetricValueSource<EventValue> evnt)
        {
            return new JsonEvent
            {
                Name = evnt.Name,
                Unit = evnt.Unit.Name,
                Events = evnt.Value.Events,
                Tags = evnt.Tags
            };
        }

        public JsonObject ToJsonObject()
        {
            return new JsonObject(ToJsonProperties());
        }

        public IEnumerable<JsonProperty> ToJsonProperties()
        {
            yield return new JsonProperty("Name", this.Name);

            if (this.Events.Count > 0)
            {
                yield return new JsonProperty("Events", this.Events.Select(i => new JsonObject(ToJsonProperties(i))));
            }

            if (this.Tags.Count > 0)
            {
                yield return new JsonProperty("Tags", this.Tags);
            }

            yield return new JsonProperty("Unit", Metrics.Unit.None.Name);
        }

        private static IEnumerable<JsonProperty> ToJsonProperties(EventDetails item)
        {
            yield return new JsonProperty("Timestamp", item.Timestamp.ToString());

            yield return new JsonProperty("Fields", item.Fields.ToDictionary(k=> (object)k.Key,kvp=> kvp.Value));
        }

        public EventValueSource ToValueSource()
        {
            var eventValue = new EventValue(this.Events);
            return new EventValueSource(this.Name, ConstantValue.Provider(eventValue), this.Tags);
        }
    }
}
