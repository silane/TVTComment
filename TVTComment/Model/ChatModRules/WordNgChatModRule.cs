using System.Collections.Generic;

namespace TVTComment.Model.ChatModRules
{
    class WordNgChatModRule:IChatModRule
    {
        public string Description => $"NGWord: {Word}";
        public IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> TargetChatCollectServiceEntries { get; }
        public string Word { get; }

        public WordNgChatModRule(IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> targetServiceEntry,string word)
        {
            TargetChatCollectServiceEntries = targetServiceEntry;
            this.Word = word.Normalize(System.Text.NormalizationForm.FormKD).ToLower();
        }
        public bool Modify(Chat chat)
        {
            if (!chat.Text.Normalize(System.Text.NormalizationForm.FormKD).ToLower().Contains(Word))
                return false;

            chat.SetNg(true);
            return true;
        }
    }
}
