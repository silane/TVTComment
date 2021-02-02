using System;

namespace TVTComment.ViewModels.ChatCollectServiceCreationOptionControl
{
    class FileChatCollectServiceCreationOptionControlViewModel : ChatCollectServiceCreationOptionControlViewModel
    {
        private string filePath = "";
        public string FilePath
        {
            get { return filePath; }
            set { SetProperty(ref filePath, value); }
        }

        private bool relativeTime = false;
        public bool RelativeTime
        {
            get { return relativeTime; }
            set { SetProperty(ref relativeTime, value); }
        }

#pragma warning disable CS0067 // イベント 'FileChatCollectServiceCreationOptionControlViewModel.Finished' は使用されていません
        public override event EventHandler Finished;
#pragma warning restore CS0067 // イベント 'FileChatCollectServiceCreationOptionControlViewModel.Finished' は使用されていません

        public FileChatCollectServiceCreationOptionControlViewModel()
        {
        }

        public override Model.ChatCollectServiceEntry.IChatCollectServiceCreationOption GetChatCollectServiceCreationOption()
        {
            if (string.IsNullOrWhiteSpace(FilePath))
                return null;
            return new Model.ChatCollectServiceEntry.FileChatCollectServiceEntry.ChatCollectServiceCreationOption(FilePath, RelativeTime);
        }
    }
}
