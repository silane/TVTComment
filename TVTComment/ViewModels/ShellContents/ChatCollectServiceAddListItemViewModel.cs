using System;

namespace TVTComment.ViewModels.ShellContents
{
    class ChatCollectServiceAddListItemViewModel
    {
        public bool IsPreset { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public Model.ChatCollectServiceEntry.IChatCollectServiceEntry ServiceEntry { get; }
        public Model.ChatCollectServiceCreationPreset Preset { get; }

        public ChatCollectServiceAddListItemViewModel(Model.ChatCollectServiceEntry.IChatCollectServiceEntry serviceEntry, Model.ChatCollectServiceCreationPreset preset)
        {
            if (preset != null && serviceEntry != preset.ServiceEntry)
                throw new ArgumentException($"{preset}.{preset.ServiceEntry} must be same as {serviceEntry}");
            IsPreset = preset != null;
            if (!IsPreset)
            {
                Title = serviceEntry.Name;
                Subtitle = serviceEntry.Description;
            }
            else
            {
                Title = preset.Name;
                Subtitle = serviceEntry.Name;
            }
            ServiceEntry = serviceEntry;
            Preset = preset;
        }
    }
}
