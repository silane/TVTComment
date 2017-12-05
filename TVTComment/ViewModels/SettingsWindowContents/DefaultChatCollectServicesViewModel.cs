using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using System.Collections.ObjectModel;

namespace TVTComment.ViewModels.ShellContents
{
    class DefaultChatCollectServicesViewModel
    {
        public DefaultChatCollectServiceViewModel[] LiveServices { get; } 
        public DefaultChatCollectServiceViewModel[] RecordServices { get; }
        
        private IList<Model.ChatCollectServiceEntry.IChatCollectServiceEntry> liveChatCollectService;
        private IList<Model.ChatCollectServiceEntry.IChatCollectServiceEntry> recordChatCollectService;

        public DefaultChatCollectServicesViewModel(Model.TVTComment model)
        {
            liveChatCollectService = model.DefaultChatCollectServiceModule.LiveChatCollectService;
            recordChatCollectService = model.DefaultChatCollectServiceModule.RecordChatCollectService;

            LiveServices = model.ChatServices.SelectMany(x=>x.ChatCollectServiceEntries).Where(x=>x.CanUseDefaultCreationOption)
                .Select(x => new DefaultChatCollectServiceViewModel(liveChatCollectService, x,liveChatCollectService.Contains(x))).ToArray();
            RecordServices = model.ChatServices.SelectMany(x => x.ChatCollectServiceEntries).Where(x => x.CanUseDefaultCreationOption)
                .Select(x => new DefaultChatCollectServiceViewModel(recordChatCollectService, x, recordChatCollectService.Contains(x))).ToArray();
        }
    }
}
