using ObservableUtils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;

namespace TVTComment.Model.ChatService
{
    class NichanChatServiceSettings
    {
        public TimeSpan ThreadUpdateInterval { get; set; } = new TimeSpan(0, 0, 15);
        public TimeSpan ThreadListUpdateInterval { get; set; } = new TimeSpan(0, 0, 1);
        public Serialization.ColorEntity ChatColor { get; set; } = new Serialization.ColorEntity() { R = 255, G = 255, B = 255 };
        public string HmKey { get; set; } = "";
        public string AppKey { get; set; } = "";
        public string UserId { get; set; } = "";
        public string Password { get; set; } = "";
        public TimeSpan PastCollectServiceBackTime { get; set; } = new TimeSpan(3, 0, 0);
    }

    class NichanChatService : IChatService
    {
        public class BoardInfo
        {
            public string Title { get; }
            public Uri Uri { get; }
            public BoardInfo(string title,Uri uri)
            {
                Title = title;
                Uri = uri;
            }
        }

        public string Name => "2ch";
        public IReadOnlyList<ChatCollectServiceEntry.IChatCollectServiceEntry> ChatCollectServiceEntries { get; }
        public IReadOnlyList<IChatTrendServiceEntry> ChatTrendServiceEntries { get; }
        public IEnumerable<BoardInfo> BoardList { get; }

        public TimeSpan ResCollectInterval => this.resCollectInterval.Value;
        public TimeSpan ThreadSearchInterval => this.threadSearchInterval.Value;
        public Color ChatColor => this.chatColor.Value;
        public string HmKey => this.nichanApiClient.Value.HmKey;
        public string AppKey => this.nichanApiClient.Value.AppKey;
        public string UserId => this.nichanApiClient.Value.UserId;
        public string Password => this.nichanApiClient.Value.Password;
        public TimeSpan PastCollectServiceBackTime => this.pastCollectServiceBackTime.Value;

        //このChatServiceに行われた設定変更が子のChatServiceEntryに伝わるようにするためにObservableValueで包む
        private ObservableValue<TimeSpan> resCollectInterval=new ObservableValue<TimeSpan>();
        private ObservableValue<TimeSpan> threadSearchInterval = new ObservableValue<TimeSpan>();
        private ObservableValue<Color> chatColor = new ObservableValue<Color>();
        private ObservableValue<Nichan.ApiClient> nichanApiClient = new ObservableValue<Nichan.ApiClient>();
        private ObservableValue<TimeSpan> pastCollectServiceBackTime = new ObservableValue<TimeSpan>();

        private NichanUtils.BoardDatabase boardDatabase;
        private NichanUtils.ThreadResolver threadResolver;

        private NichanChatServiceSettings settings;

        public NichanChatService(
            NichanChatServiceSettings settings, ChannelDatabase channelDatabase,
            string threadSettingFilePath
        )
        {
            this.settings = settings;
            
            var boardSetting = NichanUtils.ThreadSettingFileParser.Parse(threadSettingFilePath);
            boardDatabase = new NichanUtils.BoardDatabase(boardSetting.BoardEntries, boardSetting.ThreadMappingRuleEntries);
            threadResolver = new NichanUtils.ThreadResolver(channelDatabase, boardDatabase);

            this.resCollectInterval.Value = settings.ThreadUpdateInterval;
            this.threadSearchInterval.Value = settings.ThreadListUpdateInterval;
            this.chatColor.Value = Color.FromArgb(settings.ChatColor.R, settings.ChatColor.G, settings.ChatColor.B);
            this.nichanApiClient.Value = new Nichan.ApiClient(
                settings.HmKey, settings.AppKey,
                settings.UserId, settings.Password
            );
            this.pastCollectServiceBackTime.Value = settings.PastCollectServiceBackTime;

            ChatCollectServiceEntries = new ChatCollectServiceEntry.IChatCollectServiceEntry[] {
                new ChatCollectServiceEntry.DATNichanChatCollectServiceEntry(this, chatColor, resCollectInterval, threadSearchInterval, threadResolver, nichanApiClient),
                new ChatCollectServiceEntry.PastNichanChatCollectServiceEntry(this, threadResolver, pastCollectServiceBackTime),
            };
            ChatTrendServiceEntries = new IChatTrendServiceEntry[0];

            BoardList = boardDatabase.BoardList.Select(x => new BoardInfo(x.Title, x.Uri));
        }

        public void SetIntervalValues(TimeSpan resCollectInterval, TimeSpan threadSearchInterval)
        {
            this.settings.ThreadUpdateInterval = resCollectInterval;
            this.settings.ThreadListUpdateInterval = threadSearchInterval;

            this.resCollectInterval.Value = resCollectInterval;
            this.threadSearchInterval.Value = threadSearchInterval;
        }

        public void SetChatColor(Color chatColor)
        {
            this.settings.ChatColor = new Serialization.ColorEntity() { R = chatColor.R, G = chatColor.G, B = chatColor.B };
            this.chatColor.Value = chatColor;
        }

        public void SetApiParams(string hmKey, string appKey, string userId, string password)
        {
            using (this.nichanApiClient.Value)
            {
                this.settings.HmKey = hmKey;
                this.settings.AppKey = appKey;
                this.settings.UserId = userId;
                this.settings.Password = password;

                this.nichanApiClient.Value = new Nichan.ApiClient(hmKey, appKey, userId, password);
            }
        }

        public void SetPastCollectServiceBackTime(TimeSpan value)
        {
            this.settings.PastCollectServiceBackTime = value;
            this.pastCollectServiceBackTime.Value = value;
        }

        public void Dispose()
        {
        }
    }
}
