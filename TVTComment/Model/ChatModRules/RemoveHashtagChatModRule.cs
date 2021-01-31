using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TVTComment.Model.ChatCollectServiceEntry;

namespace TVTComment.Model.ChatModRules
{
    class RemoveHashtagChatModRule : IChatModRule
    {
        private static readonly Regex HashtagPattern = new Regex("[#＃](w*[一-龠_ぁ-ん_ァ-ヴーａ-ｚＡ-Ｚa-zA-Z0-9]+|[a-zA-Z0-9_]+|[a-zA-Z0-9_]w*)", RegexOptions.Compiled);

        public string Description => "ハッシュタグを削除";
        public IEnumerable<IChatCollectServiceEntry> TargetChatCollectServiceEntries { get; }

        public RemoveHashtagChatModRule(IEnumerable<IChatCollectServiceEntry> targetChatCollectServiceEntries)
        {
            TargetChatCollectServiceEntries = targetChatCollectServiceEntries;
        }

        public bool Modify(Chat chat)
        {
            var matches = HashtagPattern.Matches(chat.Text);
            if (matches.Count == 0)
            {
                return false;
            }

            var newText = matches
                .OrderByDescending(l => l.Length)
                .Aggregate(chat.Text, (s, match) => s.Replace(match.Value, string.Empty))
                .Trim();
            chat.SetText(newText);

            return true;
        }
    }
}
