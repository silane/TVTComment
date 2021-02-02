using System.Collections.Generic;
using System.Linq;

namespace TVTComment.ViewModels.ShellContents
{
    class DefaultChatCollectServicesViewModel
    {
        public DefaultChatCollectServiceViewModel[] LiveServices { get; }
        public DefaultChatCollectServiceViewModel[] RecordServices { get; }

        private readonly IList<Model.ChatCollectServiceEntry.IChatCollectServiceEntry> liveChatCollectService;
        private readonly IList<Model.ChatCollectServiceEntry.IChatCollectServiceEntry> recordChatCollectService;

        public DefaultChatCollectServicesViewModel(Model.TVTComment model)
        {
            liveChatCollectService = model.DefaultChatCollectServiceModule.LiveChatCollectService;
            recordChatCollectService = model.DefaultChatCollectServiceModule.RecordChatCollectService;

            LiveServices = model.ChatServices.SelectMany(x => x.ChatCollectServiceEntries).Where(x => x.CanUseDefaultCreationOption)
                .Select(x => new DefaultChatCollectServiceViewModel(liveChatCollectService, x, liveChatCollectService.Contains(x))).ToArray();
            RecordServices = model.ChatServices.SelectMany(x => x.ChatCollectServiceEntries).Where(x => x.CanUseDefaultCreationOption)
                .Select(x => new DefaultChatCollectServiceViewModel(recordChatCollectService, x, recordChatCollectService.Contains(x))).ToArray();
        }
    }
}
