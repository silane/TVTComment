using System;

namespace TVTComment.Model.NichanUtils
{
    class BoardEntry
    {
        public string Id { get; }
        public string Title { get; }
        public Uri Uri { get; }
        public string[] MainThreadTitleKeywords { get; }

        public BoardEntry(string id, string title, Uri uri, string[] mainThreadTitleKeywords)
        {
            if (id.Contains("\t")) throw new ArgumentException($"{nameof(id)} must not contain tab charactor", nameof(id));
            if (title.Contains("\t")) throw new ArgumentException($"{nameof(title)} must not contain tab charactor", nameof(title));

            Id = id;
            Title = title;
            Uri = uri;
            MainThreadTitleKeywords = mainThreadTitleKeywords;
        }
    }

    enum ThreadMappingRuleTarget { Flags, NSId, NId }
    class ThreadMappingRuleEntry
    {
        public ThreadMappingRuleTarget Target { get; }
        public uint Value { get; }
        public string BoardId { get; }
        public string[] ThreadTitleKeywords { get; }

        public ThreadMappingRuleEntry(ThreadMappingRuleTarget target, uint value, string boardId, string[] threadTitleKeywords)
        {
            if (boardId.Contains("\t")) throw new ArgumentException($"{nameof(boardId)} must not contain tab charactor", nameof(boardId));

            Target = target;
            Value = value;
            BoardId = boardId;
            ThreadTitleKeywords = threadTitleKeywords;
        }
    }
}
