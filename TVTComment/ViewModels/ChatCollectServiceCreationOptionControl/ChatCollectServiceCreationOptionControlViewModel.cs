using Prism.Mvvm;
using Prism.Regions;
using System;

namespace TVTComment.ViewModels.ChatCollectServiceCreationOptionControl
{
    abstract class ChatCollectServiceCreationOptionControlViewModel : BindableBase, INavigationAware
    {
        public abstract Model.ChatCollectServiceEntry.IChatCollectServiceCreationOption GetChatCollectServiceCreationOption();
        public abstract event EventHandler Finished;

        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
        }

        public virtual bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }
    }
}
