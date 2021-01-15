using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TVTComment.Model.ChatCollectServiceEntry;

namespace TVTComment.Model.ChatModRules
{
    class RemoveMentionChatModRule : IChatModRule
    {
        private static readonly Regex MentionPattern = new Regex("@[0-9a-zA-Z_]{1,15}", RegexOptions.Compiled);

        public string Description => "メンションを削除";
        public IEnumerable<IChatCollectServiceEntry> TargetChatCollectServiceEntries { get; }

        public RemoveMentionChatModRule(IEnumerable<IChatCollectServiceEntry> targetChatCollectServiceEntries)
        {
            TargetChatCollectServiceEntries = targetChatCollectServiceEntries;
        }

        public bool Modify(Chat chat)
        {
            var matches = MentionPattern.Matches(chat.Text);
            if (matches.Count == 0)
            {
                return false;
            }

            var newText = matches
                .Aggregate(chat.Text, (s, match) => s.Replace(match.Value, string.Empty))
                .Trim();
            chat.SetText(newText);

            return true;
        }
    }
}
