using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TVTComment.Model.ChatModRules
{
    internal class RemoveAnchorChatModRule : IChatModRule
    {
        private static readonly Regex AnchorPattern = new Regex(">>\\d+", RegexOptions.Compiled);

        public string Description => "アンカーを削除";
        public IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> TargetChatCollectServiceEntries { get; }

        public RemoveAnchorChatModRule(IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> targetChatCollectServiceEntries)
        {
            TargetChatCollectServiceEntries = targetChatCollectServiceEntries;
        }

        public bool Modify(Chat chat)
        {
            var match = AnchorPattern.Match(chat.Text);
            if (!match.Success)
            {
                return false;
            }

            var newText = chat.Text.Replace(match.Value, "").Trim();
            chat.SetText(newText);

            return true;
        }
    }
}
