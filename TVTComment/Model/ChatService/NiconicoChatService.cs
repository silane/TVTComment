using ObservableUtils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace TVTComment.Model
{
    class NiconicoChatService:IChatService
    {
        private static bool instanceCreated = false;

        public string Name => "ニコニコ実況";
        public IReadOnlyList<ChatCollectServiceEntry.IChatCollectServiceEntry> ChatCollectServiceEntries { get; }
        public IReadOnlyList<IChatTrendServiceEntry> ChatTrendServiceEntries { get; }

        //このChatServiceに行われた設定変更が子のChatServiceEntryに伝わるようにするためにObservableValueで包む
        //private ObservableValue<Dictionary<uint, int>> jkIdTable = new ObservableValue<Dictionary<uint, int>>();
        private ObservableValue<NiconicoUtils.NiconicoLoginSession> loginSession = new ObservableValue<NiconicoUtils.NiconicoLoginSession>();

        private NiconicoUtils.JkIdResolver jkIdResolver;
        private SettingsBase settings;

        public string UserId
        {
            get { return (string)settings["NiconicoUserId"]; }
        }
        public string UserPassword
        {
            get { return (string)settings["NiconicoPassword"]; }
        }
        public bool IsLoggedin { get; private set; }

        public static NiconicoChatService Create(SettingsBase settings, ChannelDatabase channelDatabase,string jikkyouIdTableFilePath)
        {
            System.Diagnostics.Debug.Assert(!instanceCreated, "Can't call NiconicoChatService::Create method more than once");
            instanceCreated = true;
            return new NiconicoChatService(settings, channelDatabase, jikkyouIdTableFilePath);
        }

        private NiconicoChatService(SettingsBase settings,ChannelDatabase channelDatabase,string jikkyouIdTableFilePath)
        {
            this.settings = settings;
            this.jkIdResolver = new NiconicoUtils.JkIdResolver(channelDatabase, new NiconicoUtils.JkIdTable(jikkyouIdTableFilePath));

            try
            {
                if(!string.IsNullOrWhiteSpace(UserId) && !string.IsNullOrWhiteSpace(UserPassword))
                    SetUser(UserId, UserPassword).Wait();
            }
            catch (NiconicoUtils.NiconicoLoginException) { }

            ChatCollectServiceEntries = new ChatCollectServiceEntry.IChatCollectServiceEntry[2] { new ChatCollectServiceEntry.NiconicoChatCollectServiceEntry(this,jkIdResolver, loginSession) ,new ChatCollectServiceEntry.NiconicoLogChatCollectServiceEntry(this,jkIdResolver, loginSession)};
            ChatTrendServiceEntries = new IChatTrendServiceEntry[1] { new NiconicoChatTrendServiceEntry(jkIdResolver) };
        }

        /// <summary>
        /// ニコニコのユーザーを指定しログインする
        /// 失敗した場合オブジェクトの状態は変化しない
        /// </summary>
        /// <param name="userId">ニコニコのユーザーID</param>
        /// <param name="userPassword">ニコニコのパスワード</param>
        /// <exception cref="ArgumentException"><paramref name="userId"/>または<paramref name="userPassword"/>がnull若しくはホワイトスペースだった時</exception>
        /// <exception cref="NiconicoUtils.NiconicoLoginException">ログインに失敗した時</exception>
        public async Task SetUser(string userId,string userPassword)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException($"{nameof(userId)} must not be null nor white space",nameof(userId));
            if (string.IsNullOrWhiteSpace(userPassword))
                throw new ArgumentException($"{nameof(userPassword)} must not be null nor white space", nameof(userPassword));

            //ログインしてみる
            var tmpSession = new NiconicoUtils.NiconicoLoginSession(userId, userPassword);
            await tmpSession.Login().ConfigureAwait(false);

            //成功したら設定、セッションを置き換える
            IsLoggedin = true;
            settings["NiconicoUserId"] = userId;
            settings["NiconicoPassword"] = userPassword;
            loginSession.Value?.Logout();
            loginSession.Value = tmpSession;
        }

        public void Dispose(){ }
    }
}
