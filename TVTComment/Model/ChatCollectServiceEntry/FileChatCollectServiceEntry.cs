using System;
using System.IO;

namespace TVTComment.Model.ChatCollectServiceEntry
{
    class FileChatCollectServiceEntry : IChatCollectServiceEntry
    {
        public class ChatCollectServiceCreationOption : IChatCollectServiceCreationOption
        {
            public string FilePath { get; }
            public bool RelativeTime { get; }
            public ChatCollectServiceCreationOption(string filePath, bool relativeTime)
            {
                FilePath = filePath;
                RelativeTime = relativeTime;
            }
        }

        public ChatService.IChatService Owner { get; }
        public string Id => "File";
        public string Name => "ファイル";
        public string Description => "ニコニコ実況形式のコメントファイルからコメントを表示";
        public bool CanUseDefaultCreationOption => false;

        public FileChatCollectServiceEntry(ChatService.FileChatService owner)
        {
            Owner = owner;
        }

        public ChatCollectService.IChatCollectService GetNewService(IChatCollectServiceCreationOption creationOption)
        {
            if (creationOption == null) throw new ArgumentNullException(nameof(creationOption));
            if (creationOption is not ChatCollectServiceCreationOption option) throw new ArgumentException($"Type of {nameof(creationOption)} must be {nameof(FileChatCollectServiceEntry)}.{nameof(ChatCollectServiceCreationOption)}", nameof(creationOption));
            try
            {
                return new ChatCollectService.FileChatCollectService(this, new StreamReader(option.FilePath), option.RelativeTime);
            }
            catch (ArgumentException e)
            {
                throw new ChatCollectServiceCreationException($"コメントファイルのパスが不正です: {option.FilePath}", e);
            }
            catch (FileNotFoundException e)
            {
                throw new ChatCollectServiceCreationException($"コメントファイルが見つかりません: {option.FilePath}", e);
            }
            catch (DirectoryNotFoundException e)
            {
                throw new ChatCollectServiceCreationException($"コメントファイルが見つかりません: {option.FilePath}", e);
            }
            catch (IOException e)
            {
                throw new ChatCollectServiceCreationException($"コメントファイルを開くときにIOエラーが発生しました: {option.FilePath}", e);
            }
        }
    }
}
