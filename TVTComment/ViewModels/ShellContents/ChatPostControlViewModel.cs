using ObservableUtils;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TVTComment.ViewModels.ShellContents
{
    class ChatPostControlViewModel:INotifyPropertyChanged
    {
        private Model.TVTComment model;

        private ObservableValue<byte> chatOpacity;
        public ObservableValue<byte> ChatOpacity => chatOpacity ?? (chatOpacity = model?.ChatOpacity?.MakeLinkedObservableValue(x => (byte)(x / 16), x => (byte)(x * 16)));

        public Model.ChatCollectService.IChatCollectService SelectedPostService { get; set; }
        public ObservableValue<string> PostText { get; } = new ObservableValue<string>("");
        public ObservableValue<string> PostMailText { get; } = new ObservableValue<string>("");

        private DisposableReadOnlyObservableCollection<Model.ChatCollectService.IChatCollectService> postServices;
        public DisposableReadOnlyObservableCollection<Model.ChatCollectService.IChatCollectService> PostServices => 
            postServices ?? (postServices = model.ChatCollectServiceModule?.RegisteredServices?.MakeOneWayLinkedCollectionMany(x =>
        {
            if (x.CanPost) return new[] { x };
            else return new Model.ChatCollectService.IChatCollectService[0];
        }));

        public ObservableCollection<string> PostMailTextExamples => model.ChatPostMailTextExamples;

        private ICommand postCommand;
        public ICommand PostCommand => postCommand ?? (postCommand = new DelegateCommand(PostChat));

        private ICommand addPostMailTextExampleCommand;
        public ICommand AddPostMailTextExampleCommand => addPostMailTextExampleCommand ?? (addPostMailTextExampleCommand = new DelegateCommand<string>(AddPostMailTextExample));

        private ICommand removePostMailTextExampleCommand;
        public ICommand RemovePostMailTextExampleCommand => removePostMailTextExampleCommand ?? (removePostMailTextExampleCommand = new DelegateCommand<string>(RemovePostMailTextExample));

        public InteractionRequest<Notification> AlertRequest { get; } = new InteractionRequest<Notification>();

        public event PropertyChangedEventHandler PropertyChanged;

        public ChatPostControlViewModel(Model.TVTComment model)
        {
            this.model = model;
            Initialize();
        }

        public async Task Initialize()
        {
            await model.Initialize();
            PropertyChanged(this, new PropertyChangedEventArgs(null));
        }

        private void PostChat()
        {
            if (string.IsNullOrWhiteSpace(PostText.Value))
                return;

            if (SelectedPostService == null)
            {
                AlertRequest.Raise(new Notification { Title = "TVTCommentエラー", Content = "コメントの投稿先が選択されていません" });
                return;
            }

            if (SelectedPostService is Model.ChatCollectService.NiconicoChatCollectService)
                model.ChatCollectServiceModule.PostChat(SelectedPostService, new Model.ChatCollectService.NiconicoChatCollectService.ChatPostObject(PostText.Value, PostMailText.Value));
            else
                throw new Exception("Unknown ChatCollectService to post: " + SelectedPostService.ToString());

            PostText.Value = "";
        }

        private void AddPostMailTextExample(string postMailTextExample)
        {
            if (string.IsNullOrWhiteSpace(postMailTextExample))
                return;
            PostMailTextExamples.Add(postMailTextExample);
        }

        private void RemovePostMailTextExample(string postMailTextExample)
        {
            PostMailTextExamples.Remove(postMailTextExample);
        }
    }
}
