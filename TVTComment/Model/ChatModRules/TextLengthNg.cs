using System.Collections.Generic;
using TVTComment.Model.ChatCollectServiceEntry;

namespace TVTComment.Model.ChatModRules
{
    class TextLengthNg : IChatModRule
    {
        public string Description => $"{MaxLength}以上のコメNG";
        public readonly int MaxLength;

        public IEnumerable<IChatCollectServiceEntry> TargetChatCollectServiceEntries { get; }

        public TextLengthNg(IEnumerable<IChatCollectServiceEntry> entries, int maxLength)
        {
            TargetChatCollectServiceEntries = entries;
            MaxLength = maxLength;
        }

        public bool Modify(Chat chat)
        {
            if (MaxLength > chat.Text.Length)
                return false;
            chat.SetNg(true);
            return true;
        }
    }
}
