using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVTComment.Model;

namespace TVTComment.ViewModels.ChatCollectServiceCreationOptionControl
{
    class FileChatCollectServiceCreationOptionControlViewModel:ChatCollectServiceCreationOptionControlViewModel
    {
        private string filePath="";
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

        public override event EventHandler Finished;

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
