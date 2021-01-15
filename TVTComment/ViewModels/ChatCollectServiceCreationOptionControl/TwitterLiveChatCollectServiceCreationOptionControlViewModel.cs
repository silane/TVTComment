using Prism.Commands;
using System;
using System.Windows.Input;
using TVTComment.Model.ChatCollectServiceEntry;

namespace TVTComment.ViewModels.ChatCollectServiceCreationOptionControl
{
    class TwitterLiveChatCollectServiceCreationOptionControlViewModel : ChatCollectServiceCreationOptionControlViewModel
    {
        private string searchWord;

        public string SearchWord
        {
            get { return searchWord; }
            set { SetProperty(ref searchWord, value); }
        }

        public ICommand OkCommand { get; }

        public override event EventHandler Finished;

        public TwitterLiveChatCollectServiceCreationOptionControlViewModel()
        {
            this.OkCommand = new DelegateCommand(() => Finished(this, new EventArgs()));
        }

        public override IChatCollectServiceCreationOption GetChatCollectServiceCreationOption()
        {
            if (string.IsNullOrWhiteSpace(searchWord))
                return null;
            return new TwitterLiveChatCollectServiceEntry.ChatCollectServiceCreationOption(searchWord);
        }
    }
}
