using System;
using System.Collections.Generic;
using System.Text;

namespace TVTComment.Model
{
    class TVTCommentSettings
    {
        public ChatService.NiconicoChatServiceSettings Niconico { get; set; } = new ChatService.NiconicoChatServiceSettings();
        public ChatService.NichanChatServiceSettings Nichan { get; set; } = new ChatService.NichanChatServiceSettings();
        public string[] ChatPostMailTextExamples { get; set; } = new string[0];
        public byte ChatOpacity { get; set; } = 255;
        public int ChatPreserveCount { get; set; } = 10000;
        public bool ClearChatsOnChannelChange { get; set; } = false;
        public Serialization.ChatModRuleEntity[] ChatModRules { get; set; } = new Serialization.ChatModRuleEntity[0];
        public bool UseDefaultChatCollectService { get; set; } = false;
        public string[] LiveDefaultChatCollectServices { get; set; } = new string[0];
        public string[] RecordDefaultChatCollectServices { get; set; } = new string[0];
        public ViewModels.ViewSettings View { get; set; } = new ViewModels.ViewSettings();
    }

}
