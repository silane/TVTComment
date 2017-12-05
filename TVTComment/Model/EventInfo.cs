using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model
{
    public class EventInfo
    {
        public ushort EventId { get; }
        public string EventName { get; }
        public string EventText { get; }
        public string EventExtText { get; }
        public DateTime StartTime { get; }
        public TimeSpan Duration { get; }

        public EventInfo(ushort eventId,string eventName,string eventText,string eventExtText,DateTime startTime,TimeSpan duration)
        {
            EventId = eventId;
            EventName = eventName;
            EventText = eventText;
            EventExtText = eventExtText;
            StartTime = startTime;
            Duration = duration;
        }
    }
}
