using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TVTComment.Model
{
    class ChatCollectServiceCreationPreset
    {
        public string Name { get; }
        public ChatCollectServiceEntry.IChatCollectServiceEntry ServiceEntry { get; }
        public ChatCollectServiceEntry.IChatCollectServiceCreationOption CreationOption { get; }

        public ChatCollectServiceCreationPreset(string name, ChatCollectServiceEntry.IChatCollectServiceEntry serviceEntry, ChatCollectServiceEntry.IChatCollectServiceCreationOption creationOption)
        {
            Name = name;
            ServiceEntry = serviceEntry;
            CreationOption = creationOption;
        }
    }

    class ChatCollectServiceCreationPresetModule : IDisposable
    {

        private readonly ObservableCollection<ChatCollectServiceCreationPreset> creationPresets = new ObservableCollection<ChatCollectServiceCreationPreset>();
        public ReadOnlyObservableCollection<ChatCollectServiceCreationPreset> CreationPresets { get; }

        public ChatCollectServiceCreationPresetModule(
            TVTCommentSettings settings, IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> collectServiceEntries
        )
        {
            CreationPresets = new ReadOnlyObservableCollection<ChatCollectServiceCreationPreset>(creationPresets);
        }

        public void AddCreationPreset(string name, ChatCollectServiceEntry.IChatCollectServiceEntry serviceEntry, ChatCollectServiceEntry.IChatCollectServiceCreationOption creationOption)
        {
            creationPresets.Add(new ChatCollectServiceCreationPreset(name, serviceEntry, creationOption));
        }

        public bool RemoveCreationPreset(ChatCollectServiceCreationPreset item)
        {
            return creationPresets.Remove(item);
        }

        private void SaveSettings()
        {
        }

        public void Dispose()
        {
            SaveSettings();
        }
    }
}
