using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.ChatService
{
    class FileChatService:IChatService
    {
        private static bool instanceCreated = false;

        public string Name => "ファイル";

        public IReadOnlyList<ChatCollectServiceEntry.IChatCollectServiceEntry> ChatCollectServiceEntries { get; }
        public IReadOnlyList<IChatTrendServiceEntry> ChatTrendServiceEntries { get; } = new IChatTrendServiceEntry[0];

        public static FileChatService Create(SettingsBase settings)
        {
            System.Diagnostics.Debug.Assert(!instanceCreated, "Can't call FileChatService::Create method more than once");
            instanceCreated = true;
            return new FileChatService(settings);
        }

        private FileChatService(SettingsBase settings)
        {
            ChatCollectServiceEntries = new ChatCollectServiceEntry.IChatCollectServiceEntry[1] { new ChatCollectServiceEntry.FileChatCollectServiceEntry(this) };
        }

        public void Dispose() { }
    }
}
