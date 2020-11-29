using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Configuration;
using System.Collections.Specialized;
using ObservableUtils;
using System.Reactive.Disposables;

namespace TVTComment.Model
{
    class ChatModRuleEntry
    {
        public ChatModRules.IChatModRule ChatModRule { get; set; }
        public int AppliedCount { get; set; }
        /// <summary>
        /// 最後に適用した日時(ローカル時刻)
        /// </summary>
        public DateTime? LastAppliedTime { get; set; }
    }

    class ChatModule:IDisposable
    {
        private SettingsBase settings;
        private IPCModule ipc;
        private ChatCollectServiceModule collectServiceModule;
        private ChannelInformationModule channelInformationModule;
        private IEnumerable<ChatService.IChatService> chatServices;
        private CompositeDisposable disposables = new CompositeDisposable();

        public ObservableValue<int> ChatPreserveCount { get; } = new ObservableValue<int>();
        public ObservableValue<bool> ClearChatsOnChannelChange { get; } = new ObservableValue<bool>();

        private ObservableCollection<Chat> chats = new ObservableCollection<Chat>();
        public ReadOnlyObservableCollection<Chat> Chats { get; }

        private ObservableCollection<ChatModRuleEntry> chatModRules = new ObservableCollection<ChatModRuleEntry>();
        public ReadOnlyObservableCollection<ChatModRuleEntry> ChatModRules { get; }

        public ChatModule(SettingsBase settings,IEnumerable<ChatService.IChatService> chatServices,ChatCollectServiceModule collectServiceModule,IPCModule ipc,ChannelInformationModule channelInformationModule)
        {
            Chats = new ReadOnlyObservableCollection<Chat>(chats);
            ChatModRules = new ReadOnlyObservableCollection<ChatModRuleEntry>(chatModRules);

            this.settings = settings;
            this.chatServices = chatServices;
            this.ipc = ipc;
            this.collectServiceModule = collectServiceModule;
            this.channelInformationModule = channelInformationModule;
            disposables.Add(ChatPreserveCount.Subscribe(x => onChatPreserveCountChanged()));
            disposables.Add(channelInformationModule.CurrentChannel.Subscribe(x =>
            {
                if (ClearChatsOnChannelChange.Value)
                    ClearChats();
            }));

            collectServiceModule.NewChatProduced += collectServiceModule_NewChatProduced;
            
            loadSettings();
        }

        public void AddChatModRule(ChatModRules.IChatModRule modRule)
        {
            chatModRules.Add(new ChatModRuleEntry { ChatModRule = modRule, AppliedCount = 0 });
        }

        public void RemoveChatModRule(ChatModRules.IChatModRule modRule)
        {
            chatModRules.Remove(chatModRules.First(x => x.ChatModRule == modRule));
        }

        public void ClearChats()
        {
            chats.Clear();
        }

        private async void collectServiceModule_NewChatProduced(IEnumerable<Chat> newChats)
        {
            foreach (Chat chat in newChats)
            {
                applyChatModRule(chat);

                if (chats.Count >= ChatPreserveCount.Value)
                    chats.RemoveAt(0);
                chats.Add(chat);

                if (!chat.Ng)
                {
                    IPC.IPCMessage.ChatIPCMessage msg = new IPC.IPCMessage.ChatIPCMessage { Chat = chat };
                    await ipc.Send(msg);
                }
            }
        }

        public void Dispose()
        {
            collectServiceModule.NewChatProduced -= collectServiceModule_NewChatProduced;
            saveSettings();
            disposables.Dispose();
        }

        private void applyChatModRule(Chat chat)
        {
            foreach (ChatModRuleEntry modRule in ChatModRules)
            {
                if ((modRule.ChatModRule.TargetChatCollectServiceEntries == null || modRule.ChatModRule.TargetChatCollectServiceEntries.Contains(chat.SourceService))
                    && modRule.ChatModRule.Modify(chat))
                {
                    modRule.AppliedCount++;
                    modRule.LastAppliedTime = DateTime.Now;
                }
            }
        }

        private void onChatPreserveCountChanged()
        {
            if(chats.Count>ChatPreserveCount.Value)
            {
                for (int i = 0; i < chats.Count - ChatPreserveCount.Value; i++)
                    chats.RemoveAt(0);
            }
        }

        private void loadSettings()
        {
            ChatPreserveCount.Value = (int)settings["ChatPreserveCount"];
            ClearChatsOnChannelChange.Value = (bool)settings["ClearChatsOnChannelChange"];
            var entities = (Serialization.ChatModRuleEntity[])settings["ChatModRules"] ?? new Serialization.ChatModRuleEntity[0];
            foreach(var entity in entities)
            {
                var entry = new ChatModRuleEntry { AppliedCount = entity.AppliedCount, LastAppliedTime = entity.LastAppliedTime };
                var targetService = entity.TargetChatCollectServiceEntries.Select(entryName => chatServices.SelectMany(x=>x.ChatCollectServiceEntries).Single(x=>x.Id == entryName));
                switch (entity.Type)
                {
                    case "WordNg":
                        entry.ChatModRule=new ChatModRules.WordNgChatModRule(targetService, entity.Expression);
                        break;
                    case "UserNg":
                        entry.ChatModRule = new ChatModRules.UserNgChatModRule(targetService, entity.Expression);
                        break;
                    case "IroKomeNg":
                        entry.ChatModRule = new ChatModRules.IroKomeNgChatModRule(targetService);
                        break;
                    case "JyougeKomeNg":
                        entry.ChatModRule = new ChatModRules.JyougeKomeNgChatModRule(targetService);
                        break;
                    case "JyougeIroKomeNg":
                        entry.ChatModRule = new ChatModRules.JyougeIroKomeNgChatModRule(targetService);
                        break;
                    case "RandomizeColor":
                        entry.ChatModRule = new ChatModRules.RandomizeColorChatModRule(targetService);
                        break;
                    case "SmallOnMultiLine":
                        int lineCount;
                        if(!int.TryParse(entity.Expression, out lineCount))
                            lineCount=2;
                        entry.ChatModRule = new ChatModRules.SmallOnMultiLineChatModRule(targetService,lineCount);
                        break;
                    default:
                        continue;
                }
                chatModRules.Add(entry);
            }
        }

        private void saveSettings()
        {
            settings["ChatPreserveCount"] = ChatPreserveCount.Value;
            settings["ClearChatsOnChannelChange"] = ClearChatsOnChannelChange.Value;
            settings["ChatModRules"] = chatModRules.Select(x =>
            {
                var entity = new Serialization.ChatModRuleEntity {
                    TargetChatCollectServiceEntries =x.ChatModRule.TargetChatCollectServiceEntries.Select(entry=>entry.Id).ToArray(),
                    AppliedCount = x.AppliedCount,
                    LastAppliedTime = x.LastAppliedTime
                };
                if (x.ChatModRule is ChatModRules.WordNgChatModRule)
                {
                    entity.Type = "WordNg";
                    entity.Expression = ((ChatModRules.WordNgChatModRule)x.ChatModRule).Word;
                }
                else if (x.ChatModRule is ChatModRules.UserNgChatModRule)
                {
                    entity.Type = "UserNg";
                    entity.Expression = ((ChatModRules.UserNgChatModRule)x.ChatModRule).UserId;
                }
                else if (x.ChatModRule is ChatModRules.IroKomeNgChatModRule)
                    entity.Type = "IroKomeNg";
                else if (x.ChatModRule is ChatModRules.JyougeKomeNgChatModRule)
                    entity.Type = "JyougeKomeNg";
                else if (x.ChatModRule is ChatModRules.JyougeIroKomeNgChatModRule)
                    entity.Type = "JyougeIroKomeNg";
                else if (x.ChatModRule is ChatModRules.RandomizeColorChatModRule)
                    entity.Type = "RandomizeColor";
                else if (x.ChatModRule is ChatModRules.SmallOnMultiLineChatModRule)
                {
                    entity.Type = "SmallOnMultiLine";
                    entity.Expression = ((ChatModRules.SmallOnMultiLineChatModRule)x.ChatModRule).LineCount.ToString();
                }
                else
                    throw new Exception();
                return entity;
            }).ToArray();
        }
    }
}
