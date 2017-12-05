using Prism.Interactivity.InteractionRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.ViewModels.SettingsWindowContents
{
    class ChatCollectServiceCreationPresetConfirmation : Confirmation
    {
        public Model.ChatCollectServiceCreationPreset ChatCollectServiceCreationPreset { get; set; }
    }
}
