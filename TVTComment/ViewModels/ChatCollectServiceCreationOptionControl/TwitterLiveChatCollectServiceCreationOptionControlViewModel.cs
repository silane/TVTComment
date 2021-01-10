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
            get { return this.searchWord; }
            set { SetProperty(ref this.searchWord, value); }
        }

        public ICommand OkCommand { get; }

        public override event EventHandler Finished;

        public TwitterLiveChatCollectServiceCreationOptionControlViewModel()
        {
            this.OkCommand = new DelegateCommand(() => this.Finished(this, new EventArgs()));
        }

        public override IChatCollectServiceCreationOption GetChatCollectServiceCreationOption()
        {
            if (string.IsNullOrWhiteSpace(this.searchWord))
                return null;
            return new TwitterLiveChatCollectServiceEntry.ChatCollectServiceCreationOption(this.searchWord);
        }
    }
}
