using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Interactivity.InteractionRequest;
using ObservableUtils;

namespace TVTComment.ViewModels
{
    class SettingsWindowViewModel:BindableBase
    {
        public ObservableValue<int> ChatPreserveCount { get; }
        public ShellContents.DefaultChatCollectServicesViewModel DefaultChatCollectServices { get; }
        public ObservableValue<string> NiconicoLoginStatus { get; } = new ObservableValue<string>();
        public ObservableValue<string> NiconicoUserId { get; } = new ObservableValue<string>();
        public ObservableValue<string> NiconicoPassword { get; } = new ObservableValue<string>();
        public ObservableValue<string> NichanResCollectInterval { get; } = new ObservableValue<string>();
        public ObservableValue<string> NichanThreadSearchInterval { get; } = new ObservableValue<string>();
        public ObservableValue<System.Drawing.Color> NichanChatColor { get; } = new ObservableValue<System.Drawing.Color>();
        public ObservableValue<string> NichanHmKey { get; } = new ObservableValue<string>();
        public ObservableValue<string> NichanAppKey { get; } = new ObservableValue<string>();

        public Model.ChatService.NichanChatService.BoardInfo SelectedNichanBoard { get; set; }

        public ICommand LoginNiconicoCommand { get; }
        public ICommand OpenUserScopeSettingFileLocationCommand { get; }
        public ICommand ApplyNichanSettingsCommand { get; }
        
        public InteractionRequest<Notification> AlertRequest { get; } = new InteractionRequest<Notification>();

        public SettingsWindowContents.ChatCollectServiceCreationPresetSettingControlViewModel ChatCollectServiceCreationPresetSettingControlViewModel { get; }

        private Model.TVTComment model;
        private Model.ChatService.NiconicoChatService niconico;
        private Model.ChatService.NichanChatService nichan;

        public SettingsWindowViewModel(Model.TVTComment model)
        {
            DefaultChatCollectServices = new ShellContents.DefaultChatCollectServicesViewModel(model);

            this.model = model;
            niconico = model.ChatServices.OfType<Model.ChatService.NiconicoChatService>().Single();
            nichan = model.ChatServices.OfType<Model.ChatService.NichanChatService>().Single();

            ChatCollectServiceCreationPresetSettingControlViewModel = new SettingsWindowContents.ChatCollectServiceCreationPresetSettingControlViewModel(model);

            LoginNiconicoCommand = new DelegateCommand(async () =>
              {
                  if (string.IsNullOrWhiteSpace(NiconicoUserId.Value) || string.IsNullOrWhiteSpace(NiconicoPassword.Value))
                      return;

                  try
                  {
                      await niconico.SetUser(NiconicoUserId.Value, NiconicoPassword.Value);
                      syncNiconicoUserStatus();
                  }
                  catch (Model.NiconicoUtils.NiconicoLoginException)
                  {
                      AlertRequest.Raise(new Notification { Title = "TVTCommentエラー", Content = "ニコニコへのログインに失敗しました" });
                  }
              });

            OpenUserScopeSettingFileLocationCommand = new DelegateCommand(() => model.OpenUserScopeSettingFileLocation());

            ApplyNichanSettingsCommand = new DelegateCommand(() =>
              {
                  if (string.IsNullOrWhiteSpace(NichanResCollectInterval.Value) || string.IsNullOrWhiteSpace(NichanThreadSearchInterval.Value))
                      return;
                  try
                  {
                      nichan.SetIntervalValues(
                          TimeSpan.FromSeconds(uint.Parse(NichanResCollectInterval.Value)),
                          TimeSpan.FromSeconds(uint.Parse(NichanThreadSearchInterval.Value)));
                      
                      nichan.SetChatColor(NichanChatColor.Value);

                      nichan.SetApiParams(NichanHmKey.Value, NichanAppKey.Value, nichan.UserId, nichan.Password);

                      syncNichanSettings();
                  }
                  catch (Exception e) when (e is FormatException || e is OverflowException)
                  {
                      AlertRequest.Raise(new Notification { Title = "TVTCommentエラー", Content = "2ch設定の値が不正です" });
                  }
              });

            ChatPreserveCount = model.ChatModule.ChatPreserveCount;

            syncNiconicoUserStatus();
            syncNichanSettings();
        }

        private void syncNiconicoUserStatus()
        {
            NiconicoLoginStatus.Value = niconico.IsLoggedin ? "ログイン済" : "未ログイン";
            NiconicoUserId.Value = niconico.UserId;
            NiconicoPassword.Value = niconico.UserPassword;
        }

        private void syncNichanSettings()
        {
            NichanResCollectInterval.Value = nichan.ResCollectInterval.TotalSeconds.ToString();
            NichanThreadSearchInterval.Value = nichan.ThreadSearchInterval.TotalSeconds.ToString();
            NichanChatColor.Value = nichan.ChatColor;
            NichanHmKey.Value = nichan.HmKey;
            NichanAppKey.Value = nichan.AppKey;
        }
    }
}
