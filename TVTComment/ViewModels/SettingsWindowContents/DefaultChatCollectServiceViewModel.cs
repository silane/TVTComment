using Prism.Mvvm;
using System.Collections.Generic;

namespace TVTComment.ViewModels.ShellContents
{
    class DefaultChatCollectServiceViewModel:BindableBase
    {
        private IList<Model.ChatCollectServiceEntry.IChatCollectServiceEntry> enabledServiceEntries;

        private bool isEnabled;
        public bool IsEnabled
        {
            get { return isEnabled; }
            set { SetProperty(ref isEnabled, value); update(); }
        }

        public Model.ChatCollectServiceEntry.IChatCollectServiceEntry ServiceEntry { get; }
        
        public DefaultChatCollectServiceViewModel(IList<Model.ChatCollectServiceEntry.IChatCollectServiceEntry> enableChatCollectServiceEntry,Model.ChatCollectServiceEntry.IChatCollectServiceEntry serviceEntry,bool isEnabled)
        {
            this.isEnabled=isEnabled;
            this.enabledServiceEntries = enableChatCollectServiceEntry;
            this.ServiceEntry = serviceEntry;
            update();
        }

        private void update()
        {
            if (isEnabled)
            {
                if (enabledServiceEntries.Contains(ServiceEntry)) return;
                enabledServiceEntries.Add(ServiceEntry);
            }
            else
            {
                enabledServiceEntries.Remove(ServiceEntry);
            }
        }
    }
}
