using ObservableUtils;
using System;
using System.Drawing;

namespace TVTComment.Model.ChatCollectServiceEntry
{
    class NichanChatCollectServiceEntry:IChatCollectServiceEntry
    {
        public class ChatCollectServiceCreationOption : IChatCollectServiceCreationOption
        {
            /// <summary>
            /// スレッドの選択方法を表す
            /// </summary>
            public enum ThreadSelectMethod
            {
                /// <summary>
                /// <seealso cref="Uri"/>で指定されたスレッドを固定で表示
                /// </summary>
                Fixed,
                /// <summary>
                /// <seealso cref="Uri"/>で指定された板にある<seealso cref="Keywords"/>を含んだタイトルのスレッドを表示
                /// </summary>
                Keyword,
                /// <summary>
                /// <seealso cref="Uri"/>で指定された板にある<seealso cref="Title"/>と似たタイトルのスレッドを表示
                /// </summary>
                Fuzzy,
                /// <summary>
                /// チャンネル情報から自動で決めたスレッドを表示
                /// </summary>
                Auto,
            }

            public ThreadSelectMethod Method { get; }
            public Uri Uri { get; }
            public string Title { get; }
            public string[] Keywords { get; }

            public ChatCollectServiceCreationOption(ThreadSelectMethod method,Uri uri,string title,string[] keywords)
            {
                this.Method = method;
                this.Uri = uri;
                this.Title = title;
                this.Keywords = keywords;
            }
        }

        public IChatService Owner { get; }
        public string Id => "2ch";
        public string Name => "2ch";
        public string Description => "2chのレスを表示";
        public bool CanUseDefaultCreationOption => true;

        private ObservableValue<Color> chatColor;
        private ObservableValue<TimeSpan> resCollectInterval;
        private ObservableValue<TimeSpan> threadSearchInterval;
        private NichanUtils.ThreadResolver threadResolver;

        public NichanChatCollectServiceEntry(NichanChatService owner,ObservableValue<Color> chatColor,ObservableValue<TimeSpan> resCollectInterval,ObservableValue<TimeSpan> threadSearchInterval,NichanUtils.ThreadResolver threadResolver)
        {
            this.Owner = owner;
            this.chatColor = chatColor;
            this.resCollectInterval = resCollectInterval;
            this.threadSearchInterval = threadSearchInterval;
            this.threadResolver = threadResolver;
        }

        public ChatCollectService.IChatCollectService GetNewService(IChatCollectServiceCreationOption creationOption)
        {
            creationOption = creationOption ?? new ChatCollectServiceCreationOption(ChatCollectServiceCreationOption.ThreadSelectMethod.Auto, null, null,null);

            ChatCollectServiceCreationOption co = creationOption as ChatCollectServiceCreationOption;
            if (co==null)
                throw new ArgumentException($"Type of {nameof(creationOption)} must be {nameof(NichanChatCollectServiceEntry)}.{nameof(ChatCollectServiceCreationOption)}", nameof(creationOption));

            NichanUtils.INichanThreadSelector selector=null;
            if (co.Method == ChatCollectServiceCreationOption.ThreadSelectMethod.Fixed)
                selector = new NichanUtils.FixedNichanThreadSelector(new string[1] { co.Uri.ToString() });
            else if (co.Method == ChatCollectServiceCreationOption.ThreadSelectMethod.Keyword)
                selector = new NichanUtils.KeywordNichanThreadSelector(co.Uri,co.Keywords);
            else if (co.Method == ChatCollectServiceCreationOption.ThreadSelectMethod.Fuzzy)
                selector = new NichanUtils.FuzzyNichanThreadSelector(co.Uri, co.Title);
            else if (co.Method == ChatCollectServiceCreationOption.ThreadSelectMethod.Auto)
                selector = new NichanUtils.AutoNichanThreadSelector(threadResolver);
            else
                throw new ArgumentOutOfRangeException("((ChatCollectServiceCreationOption)creationOption).Method is out of range");

            return new ChatCollectService.NichanChatCollectService(this, chatColor.Value, resCollectInterval.Value, threadSearchInterval.Value,selector);
        }
    }
}
