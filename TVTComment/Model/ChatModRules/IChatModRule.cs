using System.Collections.Generic;

namespace TVTComment.Model.ChatModRules
{
    /// <summary>
    /// チャット修正ルール
    /// 主にNG化
    /// </summary>
    interface IChatModRule
    {
        /// <summary>
        /// ユーザーに表示するルールの説明
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 適用するコメント元
        /// </summary>
        IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> TargetChatCollectServiceEntries { get; }

        /// <summary>
        /// コメントを修正する
        /// <see cref="TargetChatCollectServiceEntries"/>について条件を調べる必要はない
        /// </summary>
        /// <param name="chat">修正するコメント</param>
        /// <returns>修正を行ったか</returns>
        bool Modify(Chat chat);
    }
}
