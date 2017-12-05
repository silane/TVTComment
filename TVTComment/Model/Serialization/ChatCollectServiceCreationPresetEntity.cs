using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.Serialization
{
    [Serializable]
    public class ChatCollectServiceCreationPresetEntity:ISerializable
    {
        public string Name { get; set; }
        public string ServiceEntryId { get; set; }
        public ChatCollectServiceEntry.IChatCollectServiceCreationOption CreationOption { get; set; }

        public ChatCollectServiceCreationPresetEntity(string name, string serviceEntryId, ChatCollectServiceEntry.IChatCollectServiceCreationOption creationOption)
        {
            this.Name = name;
            this.ServiceEntryId = serviceEntryId;
            this.CreationOption = creationOption;
        }
        protected ChatCollectServiceCreationPresetEntity(SerializationInfo info, StreamingContext context)
        {
            Name = info.GetString("Name");
            ServiceEntryId = info.GetString("ServiceEntryId");
            CreationOption = (ChatCollectServiceEntry.IChatCollectServiceCreationOption)info.GetValue("CreationOption", Type.GetType(info.GetString("CreationOptionType")));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Name", Name);
            info.AddValue("ServiceEntryId", ServiceEntryId);
            info.AddValue("CreationOptionType", CreationOption.GetType().AssemblyQualifiedName);
            info.AddValue("CreationOption", CreationOption);
        }
    }
}
