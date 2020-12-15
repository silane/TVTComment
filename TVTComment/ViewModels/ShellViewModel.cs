using ObservableUtils;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace TVTComment.ViewModels
{
    class ShellViewModel : BindableBase, IDisposable
    {
        //バインドするときclassなRectがいるから
        internal class Rect
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
        }
        private Rect windowPosition;
        public Rect WindowPosition
        {
            get { return windowPosition; }
            set { SetProperty(ref windowPosition, value); }
        }

        private Model.ChannelInfo selectedChannel;
        public Model.ChannelInfo SelectedChannel
        {
            get { return selectedChannel; }
            set { SetProperty(ref selectedChannel, value); }
        }

        public DisposableReadOnlyObservableCollection<ShellContents.ChatCollectServiceAddListItemViewModel> ChatCollectServiceAddList =>
            model.ChatCollectServiceCreationPresetModule?.CreationPresets.MakeOneWayLinkedCollection(x => new ShellContents.ChatCollectServiceAddListItemViewModel(x.ServiceEntry, x))
            .ObservableConcat(model.ChatServices.SelectMany(x => x.ChatCollectServiceEntries).Select(x => new ShellContents.ChatCollectServiceAddListItemViewModel(x, null)));

        private IObservable<long> updateTimer = Observable.Interval(new TimeSpan(0, 0, 5), new SynchronizationContextScheduler(SynchronizationContext.Current));
        public DisposableReadOnlyObservableCollection<ShellContents.ChatCollectServiceViewModel> ChatCollectServices =>
            model.ChatCollectServiceModule?.RegisteredServices?.MakeOneWayLinkedCollection(x => new ShellContents.ChatCollectServiceViewModel(x, updateTimer.Select(_ => new System.Reactive.Unit())));

        public ReadOnlyObservableCollection<Model.Chat> Chats => model.ChatModule?.Chats;

        private ReadOnlyObservableValue<Model.IForceValueData> forceValueData;
        public DisposableReadOnlyObservableCollection<ShellContents.ChannelListItemViewModel> Channels =>
            model.ChannelInformationModule?.ChannelList?.MakeOneWayLinkedCollectionMany(x =>
            {
                if (x.Hidden) return null;
                return new[] { new ShellContents.ChannelListItemViewModel(x, model.ChannelInformationModule.CurrentChannel, forceValueData) };
            });

        public ReadOnlyObservableValue<DateTime?> CurrentPlayTime { get; private set; }
        public ReadOnlyObservableValue<Model.ChannelInfo> CurrentChannel { get; private set; }
        public ReadOnlyObservableValue<Model.EventInfo> CurrentEvent { get; private set; }
        public ObservableValue<bool> UseDefaultChatCollectService { get; private set; }
        
        public ObservableValue<string> WindowTitle { get; } = new ObservableValue<string>("TVTComment");
        public ObservableValue<double> WindowOpacity => BasicSettingControlViewModel?.WindowOpacity;
        public ObservableValue<bool> WindowTopmost => BasicSettingControlViewModel?.WindowTopmost;

        public Views.AttachedProperties.GridViewColumnSettingsBinder.ColumnInfo[] ChatListColumnInfos { set; get; } = new Views.AttachedProperties.GridViewColumnSettingsBinder.ColumnInfo[0];

        public ICommand ChangeChannelCommand { get; private set; }
        public ICommand AddChatCollectServiceCommand { get; private set; }
        public ICommand RemoveChatCollectServiceCommand { get; private set; }
        public ICommand ClearChatsCommand { get; private set; }
        public ICommand AddWordNgCommand { get; private set; }
        public ICommand AddUserNgCommand { get; private set; }
        public ICommand CopyCommentCommand { get; private set; }
        public ICommand CopyUserCommand { get; private set; }

        public InteractionRequest<Notification> AlertRequest { get; } = new InteractionRequest<Notification>();
        public InteractionRequest<Notifications.ChatCollectServiceCreationSettingsConfirmation> ChatCollectServiceCreationSettingsRequest { get; } = new InteractionRequest<Notifications.ChatCollectServiceCreationSettingsConfirmation>();

        private ShellContents.BasicSettingControlViewModel basicSettingControlViewModel;
        public ShellContents.BasicSettingControlViewModel BasicSettingControlViewModel
        {
            get { return basicSettingControlViewModel; }
            set { basicSettingControlViewModel = value; initializeInternal(); }
        }

        private bool initialized = false;
        private bool disposed = false;
        private CompositeDisposable disposables = new CompositeDisposable();
        private Model.TVTComment model;

        public ShellViewModel(Model.TVTComment model)
        {
            this.model = model;
            
            var rect = (System.Drawing.RectangleF)model.Settings["MainWindowPosition"];
            WindowPosition = new Rect { X = rect.X, Y = rect.Y, Width = rect.Width, Height = rect.Height };
            
            ChatListColumnInfos = ((ListViewColumnViewModel[])model.Settings["ChatListViewColumnSettings"])?.Select(x=>new Views.AttachedProperties.GridViewColumnSettingsBinder.ColumnInfo(x.Id,x.Width)).ToArray();
            OnPropertyChanged(null);

            Window mainWindow = Application.Current.MainWindow;
            mainWindow.MouseLeftButtonDown += (_, __) => { mainWindow.DragMove(); };
            
            model.ApplicationClose += CloseApplication;
        }

        private async Task initializeInternal()
        {
            if (initialized) return;
            initialized = true;
            try
            {
                await model.Initialize();
                await BasicSettingControlViewModel.Initialize();
                //await ChatPostControlViewModel.Initialize();
            }
            catch (Exception e)
            {
                AlertRequest.Raise(new Notification { Title = "TVTCommentエラー", Content = $"初期化で予期しないエラーが発生しました\n{e.ToString()}" });
                CloseApplication();
                return;
            }
            if (model.State != Model.TVTCommentState.Working)
            {
                AlertRequest.Raise(new Notification { Title = "TVTCommentエラー", Content = "Viewer側（TVTestプラグイン側）と接続できませんでした" });
                CloseApplication();
                return;
            }

            ChangeChannelCommand = new DelegateCommand<Model.ChannelInfo>(channel => { if (channel != null) model.ChannelInformationModule.SetCurrentChannel(channel); });
            AddChatCollectServiceCommand = new DelegateCommand<ShellContents.ChatCollectServiceAddListItemViewModel>(async x => { if (x != null) await addChatCollectService(x); },
                (serviceEntry) => !UseDefaultChatCollectService.Value).ObservesProperty(() => UseDefaultChatCollectService);
            RemoveChatCollectServiceCommand = new DelegateCommand<Model.ChatCollectService.IChatCollectService>(service => { if (service != null) model.ChatCollectServiceModule.RemoveService(service); },
                (serviceEntry) => !UseDefaultChatCollectService.Value).ObservesProperty(() => UseDefaultChatCollectService);
            ClearChatsCommand = new DelegateCommand(() => model.ChatModule.ClearChats());
            AddWordNgCommand = new DelegateCommand<Model.Chat>(chat =>
            {
                if (chat == null) return;
                model.ChatModule.AddChatModRule(new Model.ChatModRules.WordNgChatModRule(model.ChatServices.SelectMany(x => x.ChatCollectServiceEntries), chat.Text));
            });
            AddUserNgCommand = new DelegateCommand<Model.Chat>(chat =>
            {
                if (chat == null) return;
                model.ChatModule.AddChatModRule(new Model.ChatModRules.UserNgChatModRule(chat.SourceService.Owner.ChatCollectServiceEntries, chat.UserId));
            });
            CopyCommentCommand = new DelegateCommand<Model.Chat>(chat =>
            {
                if (chat == null) return;
                Clipboard.SetText(chat.Text);
            });
            CopyUserCommand = new DelegateCommand<Model.Chat>(chat =>
            {
                if (chat == null) return;
                Clipboard.SetText(chat.UserId);
            });

            model.ChatCollectServiceModule.ErrorOccurredInChatCollecting += model_ErrorOccurredInChatCollecting;
            model.ChatCollectServiceModule.ErrorOccurredInChatPosting += model_ErrorOccurredInChatPosting;
            model.ChatCollectServiceModule.ErrorOccurredInServiceCreation += model_ErrorOccurredAtChatCollectServiceCreation;

            CurrentPlayTime = model.ChannelInformationModule.CurrentTime;
            CurrentChannel = model.ChannelInformationModule.CurrentChannel;
            CurrentEvent = model.ChannelInformationModule.CurrentEvent;

            disposables.Add(CurrentPlayTime.Subscribe(_ => updateWindowTitle()));
            disposables.Add(CurrentChannel.Subscribe(_ => updateWindowTitle()));
            disposables.Add(CurrentEvent.Subscribe(_ => updateWindowTitle()));

            //勢い値は「ニコニコ実況」のものを固定で表示
            model.ChatTrendServiceModule.AddService(model.ChatServices.OfType<Model.ChatService.NiconicoChatService>().Single().ChatTrendServiceEntries[0]);
            forceValueData = new ReadOnlyObservableValue<Model.IForceValueData>(Observable.FromEventPattern<Model.ChatTrendServiceModule.ForceValueUpdatedEventArgs>(
                        h => model.ChatTrendServiceModule.ForceValueUpdated += h, h => model.ChatTrendServiceModule.ForceValueUpdated -= h).Select(y => y.EventArgs.ForceValueData));

            UseDefaultChatCollectService = model.DefaultChatCollectServiceModule.IsEnabled;

            model.CommandModule.ShowWindowCommandInvoked += commandModule_ShowWindowCommandInvoked;

            OnPropertyChanged(null);
        }

        private void CloseApplication()
        {
            Window window = Application.Current.MainWindow;
            window.Close();
        }

        private void updateWindowTitle()
        {
            WindowTitle.Value = $"{CurrentChannel.Value?.ServiceName} {CurrentPlayTime.Value?.ToString("yy/M/d(ddd) HH:mm:ss")} - TVTComment";
        }

        private async Task addChatCollectService(ShellContents.ChatCollectServiceAddListItemViewModel item)
        {
            if (item.IsPreset)
            {
                model.ChatCollectServiceModule.AddService(item.ServiceEntry, item.Preset.CreationOption);
            }
            else
            {
                if (item.ServiceEntry is Model.ChatCollectServiceEntry.NiconicoChatCollectServiceEntry)
                    model.ChatCollectServiceModule.AddService(item.ServiceEntry, new Model.ChatCollectServiceEntry.NiconicoChatCollectServiceEntry.ChatCollectServiceCreationOption());
                else if (item.ServiceEntry is Model.ChatCollectServiceEntry.NiconicoLogChatCollectServiceEntry)
                    model.ChatCollectServiceModule.AddService(item.ServiceEntry, new Model.ChatCollectServiceEntry.NiconicoLogChatCollectServiceEntry.ChatCollectServiceCreationOption());
                else if (
                    item.ServiceEntry is Model.ChatCollectServiceEntry.NichanChatCollectServiceEntry ||
                    item.ServiceEntry is Model.ChatCollectServiceEntry.FileChatCollectServiceEntry ||
                    item.ServiceEntry is Model.ChatCollectServiceEntry.NiconicoLiveChatCollectServiceEntry
                )
                {
                    var confirmation = await ChatCollectServiceCreationSettingsRequest.RaiseAsync(
                        new Notifications.ChatCollectServiceCreationSettingsConfirmation { Title = "コメント元設定", TargetChatCollectServiceEntry = item.ServiceEntry });
                    if (!confirmation.Confirmed) return;
                    model.ChatCollectServiceModule.AddService(item.ServiceEntry, confirmation.ChatCollectServiceCreationOption);
                }
                else
                    throw new Exception("Unknown ChatCollectServiceEntry: " + item.GetType().ToString());
            }
        }

        private void model_ErrorOccurredInChatCollecting(Model.ChatCollectService.IChatCollectService service, string errorText)
        {
            AlertRequest.Raise(new Notification { Title = "TVTCommentエラー", Content = $"\"{service.Name}\"で以下のエラーが発生しました。このコメント元を無効化します。\n\n{errorText}" });
        }

        private void model_ErrorOccurredInChatPosting(Model.ChatCollectService.IChatCollectService service, Model.ChatCollectService.BasicChatPostObject postObject, string errorText)
        {
            AlertRequest.Raise(new Notification { Title = "TVTCommentエラー", Content = $"\"{service.Name}\"にコメントを投稿するとき以下のエラーが発生しました。\n\n{errorText}" });
        }

        private void model_ErrorOccurredAtChatCollectServiceCreation(Model.ChatCollectServiceEntry.IChatCollectServiceEntry serviceEntry, string errorText)
        {
            AlertRequest.Raise(new Notification { Title = "TVTCommentエラー", Content = $"コメント元、\"{serviceEntry.Name}\"を有効にしようとしたとき以下のエラーが発生し、有効化できませんでした。\n\n{errorText}" });
        }

        private void commandModule_ShowWindowCommandInvoked()
        {
            Window window=Application.Current.MainWindow;
            //windowをアクティブにする。window.Activate()は不確実。実験した中ではこれが一番確実だった
            window.WindowState = WindowState.Minimized;
            window.WindowState = WindowState.Normal;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            if(model.CommandModule!=null)
                model.CommandModule.ShowWindowCommandInvoked -= commandModule_ShowWindowCommandInvoked;
            if (model.ChatCollectServiceModule != null)
            {
                model.ChatCollectServiceModule.ErrorOccurredInServiceCreation -= model_ErrorOccurredAtChatCollectServiceCreation;
                model.ChatCollectServiceModule.ErrorOccurredInChatPosting -= model_ErrorOccurredInChatPosting;
                model.ChatCollectServiceModule.ErrorOccurredInChatCollecting -= model_ErrorOccurredInChatCollecting;
            }
            model.Settings["MainWindowPosition"] = new System.Drawing.RectangleF((float)WindowPosition.X, (float)WindowPosition.Y, (float)WindowPosition.Width, (float)WindowPosition.Height);
            model.Settings["ChatListViewColumnSettings"] = ChatListColumnInfos.Select(x=>new ListViewColumnViewModel { Id = x.Id, Width = x.Width }).ToArray();
            model.Dispose();
            model.ApplicationClose -= CloseApplication;
            disposables.Dispose();

            disposed = true;
        }
    }
}
