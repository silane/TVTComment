using System.Collections.Generic;

namespace TVTComment.Model.ChatService
{
    class FileChatService : IChatService
    {
        public string Name => "ファイル";

        public IReadOnlyList<ChatCollectServiceEntry.IChatCollectServiceEntry> ChatCollectServiceEntries { get; }
        public IReadOnlyList<IChatTrendServiceEntry> ChatTrendServiceEntries { get; } = System.Array.Empty<IChatTrendServiceEntry>();

        public FileChatService()
        {
            ChatCollectServiceEntries = new ChatCollectServiceEntry.IChatCollectServiceEntry[1] { new ChatCollectServiceEntry.FileChatCollectServiceEntry(this) };
        }

        public void Dispose() { }
    }
}
