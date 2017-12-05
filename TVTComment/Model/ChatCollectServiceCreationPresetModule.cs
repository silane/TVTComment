using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model
{
    class ChatCollectServiceCreationPreset
    {
        public string Name { get; }
        public ChatCollectServiceEntry.IChatCollectServiceEntry ServiceEntry { get; }
        public ChatCollectServiceEntry.IChatCollectServiceCreationOption CreationOption { get; }

        public ChatCollectServiceCreationPreset(string name, ChatCollectServiceEntry.IChatCollectServiceEntry serviceEntry, ChatCollectServiceEntry.IChatCollectServiceCreationOption creationOption)
        {
            this.Name = name;
            this.ServiceEntry = serviceEntry;
            this.CreationOption = creationOption;
        }
    }

    class ChatCollectServiceCreationPresetModule:IDisposable
    {
        private SettingsBase settings;
        private IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> collectServiceEntries;

        private ObservableCollection<ChatCollectServiceCreationPreset> creationPresets = new ObservableCollection<ChatCollectServiceCreationPreset>();
        public ReadOnlyObservableCollection<ChatCollectServiceCreationPreset> CreationPresets { get; }

        public ChatCollectServiceCreationPresetModule(SettingsBase settings,IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> collectServiceEntries)
        {
            this.settings = settings;
            this.collectServiceEntries = collectServiceEntries;

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

        private void loadSettings()
        {
            creationPresets.Clear();
            creationPresets.AddRange(((Serialization.ChatCollectServiceCreationPresetEntity[])settings["ChatCollectServiceCreationPresets"])
                .Select(x=>new ChatCollectServiceCreationPreset(x.Name,collectServiceEntries.Single(entry=>entry.Id==x.ServiceEntryId),x.CreationOption)));
        }

        private void saveSettings()
        {
            settings["ChatCollectServiceCreationPresets"] = creationPresets
                .Select(x=>new Serialization.ChatCollectServiceCreationPresetEntity(x.Name,x.ServiceEntry.Id,x.CreationOption)).ToArray();
        }

        public void Dispose()
        {
            saveSettings();
        }
    }
}
