using Prism.Commands;
using System;
using System.Windows.Input;
using TVTComment.Model.ChatCollectServiceEntry;

namespace TVTComment.ViewModels.ChatCollectServiceCreationOptionControl
{
    class NiconicoLiveChatCollectServiceCreationOptionControlViewModel : ChatCollectServiceCreationOptionControlViewModel
    {
        private string liveId;

        public string LiveId
        {
            get { return this.liveId; }
            set { SetProperty(ref this.liveId, value); }
        }

        public ICommand OkCommand { get; }

        public override event EventHandler Finished;

        public NiconicoLiveChatCollectServiceCreationOptionControlViewModel()
        {
            this.OkCommand = new DelegateCommand(() => this.Finished(this, new EventArgs()));
        }

        public override IChatCollectServiceCreationOption GetChatCollectServiceCreationOption()
        {
            if (string.IsNullOrWhiteSpace(this.LiveId))
                return null;
            return new NiconicoLiveChatCollectServiceEntry.ChatCollectServiceCreationOption(this.LiveId);
        }
    }
}
