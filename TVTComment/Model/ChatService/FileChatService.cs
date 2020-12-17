using System.Collections.Generic;

namespace TVTComment.Model.ChatService
{
    class FileChatService : IChatService
    {
        public string Name => "ファイル";

        public IReadOnlyList<ChatCollectServiceEntry.IChatCollectServiceEntry> ChatCollectServiceEntries { get; }
        public IReadOnlyList<IChatTrendServiceEntry> ChatTrendServiceEntries { get; } = new IChatTrendServiceEntry[0];

        public FileChatService()
        {
            ChatCollectServiceEntries = new ChatCollectServiceEntry.IChatCollectServiceEntry[1] { new ChatCollectServiceEntry.FileChatCollectServiceEntry(this) };
        }

        public void Dispose() { }
    }
}
