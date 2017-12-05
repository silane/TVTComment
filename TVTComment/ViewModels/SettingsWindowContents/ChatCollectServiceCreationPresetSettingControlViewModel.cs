using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TVTComment.ViewModels.SettingsWindowContents
{
    class ChatCollectServiceCreationPresetSettingControlViewModel
    {
        private Model.TVTComment model;

        public Model.ChatCollectServiceCreationPreset SelectedChatCollectServiceCreationPreset { get; set; }
        public ReadOnlyObservableCollection<Model.ChatCollectServiceCreationPreset> ChatCollectServiceCreationPresets { get; }

        public ICommand AddPreset { get; }
        public ICommand RemovePreset { get; }

        public InteractionRequest<ChatCollectServiceCreationPresetConfirmation> AddPresetRequest { get; } = new InteractionRequest<ChatCollectServiceCreationPresetConfirmation>();

        public ChatCollectServiceCreationPresetSettingControlViewModel(Model.TVTComment model)
        {
            this.model = model;
            ChatCollectServiceCreationPresets = model.ChatCollectServiceCreationPresetModule.CreationPresets;

            AddPreset = new DelegateCommand(async () =>
              {
                  ChatCollectServiceCreationPresetConfirmation result = await AddPresetRequest.RaiseAsync(new ChatCollectServiceCreationPresetConfirmation { Title = "プリセット追加" });
                  if (!result.Confirmed) return;
                  model.ChatCollectServiceCreationPresetModule.AddCreationPreset(
                      result.ChatCollectServiceCreationPreset.Name,result.ChatCollectServiceCreationPreset.ServiceEntry,result.ChatCollectServiceCreationPreset.CreationOption);
              });
            RemovePreset = new DelegateCommand(() =>
              {
                  model.ChatCollectServiceCreationPresetModule.RemoveCreationPreset(SelectedChatCollectServiceCreationPreset);
              });
        }
    }
}
