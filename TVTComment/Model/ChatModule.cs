using ObservableUtils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
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

    class ChatModule : IDisposable
    {
        private readonly TVTCommentSettings settings;
        private readonly IPCModule ipc;
        private readonly ChatCollectServiceModule collectServiceModule;
        private readonly IEnumerable<ChatService.IChatService> chatServices;
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public ObservableValue<int> ChatPreserveCount { get; } = new ObservableValue<int>();
        public ObservableValue<bool> ClearChatsOnChannelChange { get; } = new ObservableValue<bool>();

        private readonly ObservableCollection<Chat> chats = new ObservableCollection<Chat>();
        public ReadOnlyObservableCollection<Chat> Chats { get; }

        private readonly ObservableCollection<ChatModRuleEntry> chatModRules = new ObservableCollection<ChatModRuleEntry>();
        public ReadOnlyObservableCollection<ChatModRuleEntry> ChatModRules { get; }

        public ChatModule(
            TVTCommentSettings settings, IEnumerable<ChatService.IChatService> chatServices,
            ChatCollectServiceModule collectServiceModule, IPCModule ipc, ChannelInformationModule channelInformationModule
        )
        {
            Chats = new ReadOnlyObservableCollection<Chat>(chats);
            ChatModRules = new ReadOnlyObservableCollection<ChatModRuleEntry>(chatModRules);

            this.settings = settings;
            this.chatServices = chatServices;
            this.ipc = ipc;
            this.collectServiceModule = collectServiceModule;
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

                if (ChatPreserveCount.Value > 0)
                {
                    while(chats.Count >= ChatPreserveCount.Value)
                        chats.RemoveAt(0);
                }
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
            ChatPreserveCount.Value = this.settings.ChatPreserveCount;
            ClearChatsOnChannelChange.Value = this.settings.ClearChatsOnChannelChange;
            var chatCollectServiceEntries = chatServices.SelectMany(x => x.ChatCollectServiceEntries).ToArray();
            var entities = this.settings.ChatModRules;
            foreach(var entity in entities)
            {
                var entry = new ChatModRuleEntry { AppliedCount = entity.AppliedCount, LastAppliedTime = entity.LastAppliedTime };
                var targetServices = entity.TargetChatCollectServiceEntries.Select(
                    entryName => chatCollectServiceEntries.SingleOrDefault(x => x.Id == entryName)
                ).Where(x => x != null);
                switch (entity.Type)
                {
                    case "WordNg":
                        entry.ChatModRule = new ChatModRules.WordNgChatModRule(targetServices, entity.Expression);
                        break;
                    case "UserNg":
                        entry.ChatModRule = new ChatModRules.UserNgChatModRule(targetServices, entity.Expression);
                        break;
                    case "IroKomeNg":
                        entry.ChatModRule = new ChatModRules.IroKomeNgChatModRule(targetServices);
                        break;
                    case "JyougeKomeNg":
                        entry.ChatModRule = new ChatModRules.JyougeKomeNgChatModRule(targetServices);
                        break;
                    case "JyougeIroKomeNg":
                        entry.ChatModRule = new ChatModRules.JyougeIroKomeNgChatModRule(targetServices);
                        break;
                    case "RandomizeColor":
                        entry.ChatModRule = new ChatModRules.RandomizeColorChatModRule(targetServices);
                        break;
                    case "SmallOnMultiLine":
                        int lineCount;
                        if(!int.TryParse(entity.Expression, out lineCount))
                            lineCount=2;
                        entry.ChatModRule = new ChatModRules.SmallOnMultiLineChatModRule(targetServices,lineCount);
                        break;
                    case "RemoveAnchor":
                        entry.ChatModRule = new ChatModRules.RemoveAnchorChatModRule(targetServices);
                        break;
                    case "RemoveUrl":
                        entry.ChatModRule = new ChatModRules.RemoveUrlChatModRule(targetServices);
                        break;
                    case "RenderEmotionAsComment":
                        entry.ChatModRule = new ChatModRules.RenderEmotionAsCommentChatModRule(targetServices);
                        break;
                    case "RenderInfoAsComment":
                        entry.ChatModRule = new ChatModRules.RenderInfoAsCommentChatModRule(targetServices);
                        break;
                    case "SetColor":
                        string[] splited = entity.Expression.Split(',');
                        byte[] components = splited.Length == 4
                                            ? splited.Select(x => byte.TryParse(x, out byte num) ? num : (byte)255).ToArray()
                                            : new byte[] { 255, 255, 255, 255 };
                        entry.ChatModRule = new ChatModRules.SetColorChatModRule(
                            targetServices, Color.FromArgb(components[0], components[1], components[2], components[3])
                        );
                        break;
                    case "RenderNicoadAsComment":
                        entry.ChatModRule = new ChatModRules.RenderNicoadAsCommentChatModRule(targetServices);
                        break;
                    default:
                        continue;
                }
                chatModRules.Add(entry);
            }
        }

        private void saveSettings()
        {
            this.settings.ChatPreserveCount = ChatPreserveCount.Value;
            this.settings.ClearChatsOnChannelChange = ClearChatsOnChannelChange.Value;
            this.settings.ChatModRules = chatModRules.Select(x =>
            {
                var entity = new Serialization.ChatModRuleEntity {
                    TargetChatCollectServiceEntries = x.ChatModRule.TargetChatCollectServiceEntries.Select(entry=>entry.Id).ToArray(),
                    AppliedCount = x.AppliedCount,
                    LastAppliedTime = x.LastAppliedTime
                };

                switch(x.ChatModRule)
                {
                    case ChatModRules.WordNgChatModRule wordNg:
                        entity.Type = "WordNg";
                        entity.Expression = wordNg.Word;
                        break;
                    case ChatModRules.UserNgChatModRule userNg:
                        entity.Type = "UserNg";
                        entity.Expression = userNg.UserId;
                        break;
                    case ChatModRules.IroKomeNgChatModRule _:
                        entity.Type = "IroKomeNg";
                        break;
                    case ChatModRules.JyougeKomeNgChatModRule _:
                        entity.Type = "JyougeKomeNg";
                        break;
                    case ChatModRules.JyougeIroKomeNgChatModRule _:
                        entity.Type = "JyougeIroKomeNg";
                        break;
                    case ChatModRules.RandomizeColorChatModRule _:
                        entity.Type = "RandomizeColor";
                        break;
                    case ChatModRules.SmallOnMultiLineChatModRule smallOnMultiLine:
                        entity.Type = "SmallOnMultiLine";
                        entity.Expression = smallOnMultiLine.LineCount.ToString();
                        break;
                    case ChatModRules.RemoveAnchorChatModRule _:
                        entity.Type = "RemoveAnchor";
                        break;
                    case ChatModRules.RemoveUrlChatModRule _:
                        entity.Type = "RemoveUrl";
                        break;
                    case ChatModRules.RenderEmotionAsCommentChatModRule _:
                        entity.Type = "RenderEmotionAsComment";
                        break;
                    case ChatModRules.RenderInfoAsCommentChatModRule _:
                        entity.Type = "RenderInfoAsComment";
                        break;
                    case ChatModRules.SetColorChatModRule setColor:
                        entity.Type = "SetColor";
                        Color color = setColor.Color;
                        entity.Expression = $"{color.A},{color.R},{color.G},{color.B}";
                        break;
                    case ChatModRules.RenderNicoadAsCommentChatModRule _:
                        entity.Type = "RenderNicoadAsComment";
                        break;
                    default:
                        throw new Exception();
                }
                return entity;
            }).ToArray();
        }
    }
}
