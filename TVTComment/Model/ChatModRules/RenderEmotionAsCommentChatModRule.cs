using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TVTComment.Model.ChatModRules
{
    internal class RenderEmotionAsCommentChatModRule : IChatModRule
    {
        private static readonly Regex Pattern = new Regex("^/emotion (?<text>.+)$", RegexOptions.Compiled);
        private static readonly Regex CountPattern = new Regex(@"/emotion (?<text>.+)×(?<count>\d+)$", RegexOptions.Compiled);

        public string Description => "/emotion コマンドを通常コメントとして描画";
        public IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> TargetChatCollectServiceEntries { get; }

        public RenderEmotionAsCommentChatModRule(IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> targetChatCollectServiceEntries)
        {
            TargetChatCollectServiceEntries = targetChatCollectServiceEntries;
        }

        public bool Modify(Chat chat)
        {
            var countMatch = CountPattern.Match(chat.Text);
            if (countMatch.Success)
            {
                var n = int.Parse(countMatch.Groups["count"].Value);
                var newText = string.Concat(Enumerable.Repeat(countMatch.Groups["text"].Value, n));
                chat.SetText(newText);
                return true;
            }

            var match = Pattern.Match(chat.Text);
            if (match.Success)
            {
                var newText = match.Groups["text"].Value;
                chat.SetText(newText);
                return true;
            }

            return false;
        }
    }
}
