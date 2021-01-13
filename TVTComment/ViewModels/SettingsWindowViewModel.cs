using ObservableUtils;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using System;
using System.Linq;
using System.Windows.Input;

namespace TVTComment.ViewModels
{
    class SettingsWindowViewModel : BindableBase
    {
        public ObservableValue<int> ChatPreserveCount { get; }
        public ShellContents.DefaultChatCollectServicesViewModel DefaultChatCollectServices { get; }
        public ObservableValue<string> NiconicoLoginStatus { get; } = new ObservableValue<string>();
        public ObservableValue<string> NiconicoUserId { get; } = new ObservableValue<string>();
        public ObservableValue<string> NiconicoPassword { get; } = new ObservableValue<string>();
        public ObservableValue<string> NichanResCollectInterval { get; } = new ObservableValue<string>();
        public ObservableValue<string> NichanThreadSearchInterval { get; } = new ObservableValue<string>();
        public ObservableValue<string> NichanApiHmKey { get; } = new ObservableValue<string>();
        public ObservableValue<string> NichanApiAppKey { get; } = new ObservableValue<string>();
        public ObservableValue<string> NichanApiAuthUserAgent { get; } = new ObservableValue<string>();
        public ObservableValue<string> NichanApiAuthX2chUA { get; } = new ObservableValue<string>();
        public ObservableValue<string> NichanApiUserAgent { get; } = new ObservableValue<string>();
        public ObservableValue<string> NichanPastCollectServiceBackTime { get; } = new ObservableValue<string>();

        public Model.ChatService.NichanChatService.BoardInfo SelectedNichanBoard { get; set; }

        public ICommand LoginNiconicoCommand { get; }
        public ICommand ApplyNichanSettingsCommand { get; }
        
        public InteractionRequest<Notification> AlertRequest { get; } = new InteractionRequest<Notification>();

        public SettingsWindowContents.ChatCollectServiceCreationPresetSettingControlViewModel ChatCollectServiceCreationPresetSettingControlViewModel { get; }

        private readonly Model.ChatService.NiconicoChatService niconico;
        private readonly Model.ChatService.NichanChatService nichan;

        public SettingsWindowViewModel(Model.TVTComment model)
        {
            DefaultChatCollectServices = new ShellContents.DefaultChatCollectServicesViewModel(model);

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
                  catch (Model.NiconicoUtils.NiconicoLoginSessionException)
                  {
                      AlertRequest.Raise(new Notification { Title = "TVTCommentエラー", Content = "ニコニコへのログインに失敗しました" });
                  }
              });

            ApplyNichanSettingsCommand = new DelegateCommand(() =>
              {
                  if (string.IsNullOrWhiteSpace(NichanResCollectInterval.Value) || string.IsNullOrWhiteSpace(NichanThreadSearchInterval.Value))
                      return;
                  try
                  {
                      nichan.SetIntervalValues(
                          TimeSpan.FromSeconds(uint.Parse(NichanResCollectInterval.Value)),
                          TimeSpan.FromSeconds(uint.Parse(NichanThreadSearchInterval.Value)));
                      
                      nichan.SetApiParams(
                          NichanApiHmKey.Value, NichanApiAppKey.Value, nichan.GochanApiUserId, nichan.GochanApiPassword,
                          NichanApiAuthUserAgent.Value, NichanApiAuthX2chUA.Value, NichanApiUserAgent.Value
                      );

                      nichan.SetPastCollectServiceBackTime(TimeSpan.FromMinutes(double.Parse(NichanPastCollectServiceBackTime.Value)));

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
            NichanApiHmKey.Value = nichan.GochanApiHmKey;
            NichanApiAppKey.Value = nichan.GochanApiAppKey;
            NichanApiAuthUserAgent.Value = nichan.GochanApiAuthUserAgent;
            NichanApiAuthX2chUA.Value = nichan.GochanApiAuthX2UA;
            NichanApiUserAgent.Value = nichan.GochanApiUserAgent;
            NichanPastCollectServiceBackTime.Value = nichan.PastCollectServiceBackTime.TotalMinutes.ToString();
        }
    }
}
