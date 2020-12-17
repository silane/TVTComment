using ObservableUtils;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;

namespace TVTComment.ViewModels.ShellContents
{
    class BasicSettingControlViewModel:IDisposable,INotifyPropertyChanged
    {
        private Model.TVTComment model;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Version { get; }
        public ObservableValue<byte> ChatOpacity { get; private set; }
        public ObservableValue<double> WindowOpacity { get; } = new ObservableValue<double>(1.0);
        public ObservableValue<bool> WindowTopmost { get; } = new ObservableValue<bool>(false);
        public ObservableValue<TimeSpan> ChatCollectTimeAdjustment { get; private set; }
        public ObservableValue<bool> ClearChatsOnChannelChange { get; private set; }

        public DelegateCommand<int?> SetChatCollectTimeAdjustment { get; private set; }
        public DelegateCommand<int?> AddChatCollectTimeAdjustment { get; private set; }

        public BasicSettingControlViewModel(Model.TVTComment model)
        {
            this.model = model;

            var assembly = Assembly.GetExecutingAssembly().GetName();
            this.Version = assembly.Version.ToString(3);
        }

        public async Task Initialize()
        {
            await model.Initialize();
            //modelの初期化エラーへの対処はShellViewModelでするので全部無視    
            if (model.State != Model.TVTCommentState.Working) return;

            WindowTopmost.Value = model.Settings.View.WindowTopmost;
            WindowOpacity.Value = model.Settings.View.WindowOpacity;

            //256段階でスライダーを動かすと大量にSetChatOpacityIPCMessageが発生してしまうため16段階にする
            ChatOpacity = model.ChatOpacity.MakeLinkedObservableValue(x => (byte)(x / 16), x => (byte)(x * 16));
            ClearChatsOnChannelChange = model.ChatModule.ClearChatsOnChannelChange;

            ChatCollectTimeAdjustment = model.ChatCollectServiceModule.TimeAdjustment;

            AddChatCollectTimeAdjustment = new DelegateCommand<int?>(time => { if(time.HasValue) ChatCollectTimeAdjustment.Value = ChatCollectTimeAdjustment.Value.Add(TimeSpan.FromSeconds(time.Value)); },_=>true);
            SetChatCollectTimeAdjustment = new DelegateCommand<int?>(time => { if(time.HasValue) ChatCollectTimeAdjustment.Value = TimeSpan.FromSeconds(time.Value); },_=>true);

            PropertyChanged(this, new PropertyChangedEventArgs(null));
        }

        public void Dispose()
        {
            model.Settings.View.WindowTopmost = WindowTopmost.Value;
            model.Settings.View.WindowOpacity= WindowOpacity.Value;
        }
    }
}
