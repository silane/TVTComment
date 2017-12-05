using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.ChatModRules
{
    class UserNgChatModRule:IChatModRule
    {
        public string Description => $"NGUser: {UserId}";
        public IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> TargetChatCollectServiceEntries { get; }
        public string UserId { get; }

        public UserNgChatModRule(IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> targetServiceEntry,string userId)
        {
            this.TargetChatCollectServiceEntries = targetServiceEntry;
            this.UserId = userId;
        }

        public bool Modify(Chat chat)
        {
            if (chat.UserId != UserId)
                return false;

            chat.SetNg(true);
            return true;
        }
    }
}
