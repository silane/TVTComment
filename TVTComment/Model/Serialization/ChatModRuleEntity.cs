using System;

namespace TVTComment.Model.Serialization
{
    [Serializable]
    public class ChatModRuleEntity
    {
        public string Type { get; set; }
        public string Expression { get; set; }
        public string[] TargetChatCollectServiceEntries { get; set; }
        public DateTime? LastAppliedTime { get; set; }
        public int AppliedCount { get; set; }
    }
}
