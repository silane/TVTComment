using ObservableUtils;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TVTComment.Model
{
    enum TVTCommentState
    {
        NotInitialized,
        Initializing,
        Working,
        Disposing,
        Disposed,
    }

    class TVTComment : IDisposable
    {
        private Task initializeTask;
        private ChannelDatabase channelDatabase;

        private IPCModule ipcModule;
        public ChannelInformationModule ChannelInformationModule { get; private set; }
        public ChatCollectServiceModule ChatCollectServiceModule { get; private set; }
        public ChatTrendServiceModule ChatTrendServiceModule { get; private set; }
        public ChatModule ChatModule { get; private set; }
        public DefaultChatCollectServiceModule DefaultChatCollectServiceModule { get; private set; }
        public CommandModule CommandModule { get; private set; }
        public ChatCollectServiceCreationPresetModule ChatCollectServiceCreationPresetModule { get; private set; }

        public TVTCommentState State { get; private set; }

        /// <summary>
        /// trueなら<see cref="Dispose"/>でViewer側との切断時手続きを行わない
        /// </summary>
        private bool quickDispose = false;
        /// <summary>
        /// 相手からのClose要求を受けているならtrue
        /// </summary>
        private bool isClosing = false;
        /// <summary>
        /// こちらからClose要求をした時に相手からの返事を待つのに使う
        /// 返事を待っている期間だけresetになる
        /// </summary>
        private ManualResetEventSlim closingResetEvent = new ManualResetEventSlim(true);
        private readonly SettingFileReaderWriter<TVTCommentSettings> settingReaderWriter;

        public TVTCommentSettings Settings { get; private set; }
        //public SettingsBase Settings => Properties.Settings.Default;
        public ReadOnlyCollection<ChatService.IChatService> ChatServices { get; private set; }
        public ObservableValue<byte> ChatOpacity { get; private set; }
        public ObservableCollection<string> ChatPostMailTextExamples { get; } = new ObservableCollection<string>();

        /// <summary>
        /// アプリを閉じたいときに<see cref="Initialize"/>を呼んだのと同じ同期コンテキスト上で呼ばれるので、絶対に登録し、thisのDisposeを呼ぶようにする
        /// </summary>
        public event Action ApplicationClose;
      
        public TVTComment()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            string baseDir = Path.GetDirectoryName(getExePath());
            this.settingReaderWriter = new SettingFileReaderWriter<TVTCommentSettings>(Path.Combine(baseDir, "settings"), true);

            State = TVTCommentState.NotInitialized;
        }

        /// <summary>
        /// Viewerとの接続などの初期化を行う
        /// 接続処理に失敗しても例外は投げないが、その他の例外は普通に投げる
        /// 前者の場合再度呼び出せる
        /// </summary>
        public Task Initialize()
        {
            return initializeTask ?? (initializeTask = initialize());
        }

        private async Task initialize()
        {
            if (State != TVTCommentState.NotInitialized)
                throw new InvalidOperationException("This object is already initialized");

            // プラグインの無効化→有効化を短時間で行うと
            // 設定ファイルがアクセス中でIOExceptionが飛ぶので時間を空けて試す
            for (int i = 1; ; ++i)
            {
                try
                {
                    this.Settings = await this.settingReaderWriter.Read();
                    break;
                }
                catch (FormatException)
                {
                    this.Settings = new TVTCommentSettings();
                    break;
                }
                catch (IOException)
                {
                    const int retryCount = 6;
                    if(i >= retryCount)
                        throw;
                }
                await Task.Delay(500);
            }

            string baseDir = Path.GetDirectoryName(getExePath());
            this.channelDatabase = new ChannelDatabase(Path.Combine(baseDir, "channels.txt"));
            this.ChatServices = new ReadOnlyCollection<ChatService.IChatService>(new ChatService.IChatService[] {
                new ChatService.NiconicoChatService(Settings.Niconico, this.channelDatabase, Path.Combine(baseDir, "niconicojikkyouids.txt")),
                new ChatService.NichanChatService(Settings.Nichan, this.channelDatabase, Path.Combine(baseDir, "2chthreads.txt")),
                new ChatService.FileChatService()
            });

            var chatCollectServiceEntryIds = this.ChatServices.SelectMany(x => x.ChatCollectServiceEntries).Select(x => x.Id);
            System.Diagnostics.Debug.Assert(
                chatCollectServiceEntryIds.Distinct().Count() == chatCollectServiceEntryIds.Count(),
                "IDs of ChatCollectServiceEntries are not unique"
            );

            //Viewerとの接続
            string[] commandLine = Environment.GetCommandLineArgs();
            if (commandLine.Length == 3)
                ipcModule = new IPCModule(commandLine[1], commandLine[2],SynchronizationContext.Current);
            else
                ipcModule = new IPCModule("TVTComment_Up", "TVTComment_Down",SynchronizationContext.Current);

            ipcModule.Disposed -= ipcManager_Disposed;
            ipcModule.Disposed += ipcManager_Disposed;
            try
            {
                await ipcModule.Connect();
            }
            catch (IPCModule.ConnectException) { return; }
            ipcModule.MessageReceived += ipcManager_MessageReceived;

            //各種SubModule作成
            ChannelInformationModule = new ChannelInformationModule(ipcModule);
            ChatCollectServiceModule = new ChatCollectServiceModule(ChannelInformationModule);
            ChatTrendServiceModule = new ChatTrendServiceModule(SynchronizationContext.Current);
            ChatModule = new ChatModule(
                this.Settings, ChatServices,ChatCollectServiceModule, ipcModule,ChannelInformationModule
            );
            DefaultChatCollectServiceModule = new DefaultChatCollectServiceModule(
                this.Settings, ChannelInformationModule, ChatCollectServiceModule,ChatServices.SelectMany(x=>x.ChatCollectServiceEntries)
            );
            CommandModule = new CommandModule(
                ipcModule, SynchronizationContext.Current
            );
            ChatCollectServiceCreationPresetModule = new ChatCollectServiceCreationPresetModule(
                this.Settings, ChatServices.SelectMany(x => x.ChatCollectServiceEntries)
            );

            //コメント透過度設定処理
            ChatOpacity = new ObservableValue<byte>(this.Settings.ChatOpacity);
            ChatOpacity.Subscribe(async opacity =>
            {
                this.Settings.ChatOpacity = opacity;
                await ipcModule.Send(new IPC.IPCMessage.SetChatOpacityIPCMessage { Opacity = opacity });
            });

            //メール欄例設定
            var chatPostMailTextExamples = this.Settings.ChatPostMailTextExamples;
            ChatPostMailTextExamples.AddRange(chatPostMailTextExamples);
            
            ipcModule.StartReceiving();
            State = TVTCommentState.Working;
        }

        private void ipcManager_Disposed(Exception exception)
        {
            if (ipcModule.DisposeReason == IPCModuleDisposeReason.DisposeCalled)
                return;

            if(ipcModule.DisposeReason==IPCModuleDisposeReason.ConnectionTerminated)
            {
                //Viewer側から切断するのは異常
                quickDispose = true;
                ApplicationClose();
            }
            else
            {
                quickDispose = true;
                ApplicationClose();
            }
        }

        private async void ipcManager_MessageReceived(IPC.IPCMessage.IIPCMessage message)
        {
            if(message is IPC.IPCMessage.SetChatOpacityIPCMessage)
            {
                await ipcModule.Send(message);
            }
            else if(message is IPC.IPCMessage.CloseIPCMessage)
            {
                isClosing = true;
                if (!closingResetEvent.IsSet)
                    //Closeの返事を待っていた
                    closingResetEvent.Set();
                else
                    //Closeの返事を待っていたわけではない->相手がCloseした。
                    ApplicationClose();//thisがDisposeされる
            }
        }

        public void OpenUserScopeSettingFileLocation()
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
            if(File.Exists(config.FilePath))
                System.Diagnostics.Process.Start("explorer.exe", $"/select, {config.FilePath}");
        }

        public void Dispose()
        {
            State = TVTCommentState.Disposing;
            if (!quickDispose)
            {
                try
                {
                    //相手からCloseを要求を受けていないなら
                    if (!isClosing)
                    {
                        closingResetEvent.Reset();
                        //CloseIPCMessageを相手に送る
                        ipcModule.Send(new IPC.IPCMessage.CloseIPCMessage()).Wait();
                        //相手からCloseIPCMessageが来るまで待つ、1秒以内に来なかったら無視して進める
                        closingResetEvent.Wait(1000);
                    }
                }
                catch { }
            }

            foreach(var chatService in ChatServices)
            {
                chatService.Dispose();
            }

            //メール欄例保存
            this.Settings.ChatPostMailTextExamples = this.ChatPostMailTextExamples.ToArray();

            //各種SubModule破棄
            CommandModule?.Dispose();
            ChatCollectServiceCreationPresetModule?.Dispose();
            DefaultChatCollectServiceModule?.Dispose();
            ChatModule?.Dispose();
            ChatTrendServiceModule?.Dispose();
            ChatCollectServiceModule?.Dispose();
            if(ipcModule!=null)
            {
                ipcModule.Disposed -= ipcManager_Disposed;
                ipcModule.MessageReceived -= ipcManager_MessageReceived;
                ipcModule.Dispose();
            }

            // 設定保存。asyncだがawaitせずに例外は無視。
            this.settingReaderWriter.Write(this.Settings).Wait(5000);

            State = TVTCommentState.Disposed;
        }

        private static string getExePath()
        {
            return System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
        }
    }
}
