using ObservableUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Interactivity.InteractionRequest;
using System.Windows.Input;
using Prism.Commands;
using TVTComment.ViewModels.Contents;
using Prism.Regions;

namespace TVTComment.ViewModels
{
    class ChatCollectServiceCreationPresetControlViewModel:IInteractionRequestAware
    {
        private IRegionManager regionManager;
        private Model.TVTComment model;
        private SettingsWindowContents.ChatCollectServiceCreationPresetConfirmation confirmation;

        public IReadOnlyList<SelectableViewModel<Model.ChatCollectServiceEntry.IChatCollectServiceEntry>> ChatCollectServiceEntries { get; }
        public ObservableValue<string> PresetName { get; } = new ObservableValue<string>();
        public ChatCollectServiceCreationOptionControl.ChatCollectServiceCreationOptionControlViewModel OptionRegionViewModel { get; set; }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public INotification Notification
        {
            get { return confirmation; }
            set { confirmation = (SettingsWindowContents.ChatCollectServiceCreationPresetConfirmation)value; initialize(); }
        }

        public Action FinishInteraction { get; set; }
        
        public ChatCollectServiceCreationPresetControlViewModel(IRegionManager regionManager,Model.TVTComment model)
        {
            this.regionManager = regionManager;
            this.model = model;
            ChatCollectServiceEntries = model.ChatServices.SelectMany(x => x.ChatCollectServiceEntries).Select(x=>new SelectableViewModel<Model.ChatCollectServiceEntry.IChatCollectServiceEntry>(x)).ToList();
            foreach (var entry in ChatCollectServiceEntries)
                entry.PropertyChanged += ChatCollectServiceEntryListItemChanged;
            ChatCollectServiceEntries.First().IsSelected = true;
            
            OkCommand = new DelegateCommand(() =>
              {
                  var entry = ChatCollectServiceEntries.FirstOrDefault(x => x.IsSelected)?.Value;
                  var option = OptionRegionViewModel?.GetChatCollectServiceCreationOption();
                  if (string.IsNullOrWhiteSpace(PresetName.Value) || entry == null || option==null) return;
                  confirmation.ChatCollectServiceCreationPreset = new Model.ChatCollectServiceCreationPreset(PresetName.Value, entry, option);
                  confirmation.Confirmed = true;
                  FinishInteraction();
              });
            CancelCommand = new DelegateCommand(() => { confirmation.Confirmed = false; FinishInteraction(); });
        }

        private void initialize()
        {
            confirmation.Confirmed = false;
            confirmation.ChatCollectServiceCreationPreset = null;
        }

        private void ChatCollectServiceEntryListItemChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectableViewModel<Model.ChatCollectServiceEntry.IChatCollectServiceEntry>.IsSelected))
                serviceEntryChanged();
        }

        private void serviceEntryChanged()
        {
            var entry = ChatCollectServiceEntries.FirstOrDefault(x => x.IsSelected)?.Value;
            if (entry is Model.ChatCollectServiceEntry.NichanChatCollectServiceEntry)
            {
                regionManager.RequestNavigate("ChatCollectServiceCreationPresetControl_OptionRegion", "NichanChatCollectServiceCreationOptionControl");
            }
            else if (entry is Model.ChatCollectServiceEntry.FileChatCollectServiceEntry)
            {
                regionManager.RequestNavigate("ChatCollectServiceCreationPresetControl_OptionRegion", "FileChatCollectServiceCreationOptionControl");
            }
        }
    }
}
