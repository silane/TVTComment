using ObservableUtils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Linq;
using System.Configuration;
using System.Drawing;

namespace TVTComment.Model
{
    class NichanChatService:IChatService
    {
        private static bool instanceCreated=false;

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

        public TimeSpan ResCollectInterval => resCollectInterval.Value;
        public TimeSpan ThreadSearchInterval => threadSearchInterval.Value;
        public Color ChatColor => chatColor.Value;

        //このChatServiceに行われた設定変更が子のChatServiceEntryに伝わるようにするためにObservableValueで包む
        private ObservableValue<TimeSpan> resCollectInterval=new ObservableValue<TimeSpan>();
        private ObservableValue<TimeSpan> threadSearchInterval = new ObservableValue<TimeSpan>();
        private ObservableValue<Color> chatColor = new ObservableValue<Color>();
        private ObservableValue<Nichan.ApiClient> nichanApiClient = new ObservableValue<Nichan.ApiClient>();

        private NichanUtils.BoardDatabase boardDatabase;
        private NichanUtils.ThreadResolver threadResolver;

        private SettingsBase settings;

        public static NichanChatService Create(SettingsBase settings, ChannelDatabase channelDatabase, string threadSettingFilePath)
        {
            System.Diagnostics.Debug.Assert(!instanceCreated, "Can't call NichanChatService::Create method more than once");
            instanceCreated = true;
            return new NichanChatService(settings, channelDatabase, threadSettingFilePath);
        }

        private NichanChatService(SettingsBase settings,ChannelDatabase channelDatabase,string threadSettingFilePath)
        {
            this.settings = settings;
            
            var boardSetting = NichanUtils.ThreadSettingFileParser.Parse(threadSettingFilePath);
            boardDatabase = new NichanUtils.BoardDatabase(boardSetting.BoardEntries, boardSetting.ThreadMappingRuleEntries);
            threadResolver = new NichanUtils.ThreadResolver(channelDatabase, boardDatabase);

            resCollectInterval.Value = (TimeSpan)settings["NichanResCollectInterval"];
            threadSearchInterval.Value = (TimeSpan)settings["NichanThreadSearchInterval"];
            chatColor.Value = (Color)(settings["NichanChatColor"] ?? Color.Empty);
            this.nichanApiClient.Value = new Nichan.ApiClient(
                (string)settings["NichanHMKey"], (string)settings["NichanAppKey"],
                (string)settings["NichanUserID"], (string)settings["NichanPassword"]
            );

            ChatCollectServiceEntries = new ChatCollectServiceEntry.IChatCollectServiceEntry[] {
                new ChatCollectServiceEntry.HTMLNichanChatCollectServiceEntry(this, chatColor, resCollectInterval, threadSearchInterval, threadResolver),
                new ChatCollectServiceEntry.DATNichanChatCollectServiceEntry(this, chatColor, resCollectInterval, threadSearchInterval, threadResolver, nichanApiClient),
            };
            ChatTrendServiceEntries = new IChatTrendServiceEntry[0];

            BoardList = boardDatabase.BoardList.Select(x => new BoardInfo(x.Title, x.Uri));
        }

        public void SetIntervalValues(TimeSpan resCollectInterval,TimeSpan threadSearchInterval)
        {
            settings["NichanResCollectInterval"] = resCollectInterval;
            settings["NichanThreadSearchInterval"] = threadSearchInterval;

            this.resCollectInterval.Value = resCollectInterval;
            this.threadSearchInterval.Value = threadSearchInterval;
        }

        public void SetChatColor(Color chatColor)
        {
            settings["NichanChatColor"] = chatColor;
            this.chatColor.Value = chatColor;
        }

        public void SetApiParams(string hmKey, string appKey, string userId, string password)
        {
            settings["NichanHMKey"] = hmKey;
            settings["NichanAppKey"] = appKey;
            settings["NichanUserID"] = userId;
            settings["NichanPassword"] = password;

            this.nichanApiClient.Value = new Nichan.ApiClient(hmKey, appKey, userId, password);
        }

        public void Dispose()
        {
        }
    }
}
