using Prism.Interactivity.InteractionRequest;

namespace TVTComment.ViewModels.Notifications
{
    /// <summary>
    /// コメント元を追加するときに表示されるダイアログ<see cref="ChatCollectServiceCreationSettingsControlViewModel"/>に渡す情報
    /// </summary>
    class ChatCollectServiceCreationSettingsConfirmation : Confirmation
    {
        /// <summary>
        /// どのコメント元を追加するか
        /// </summary>
        public Model.ChatCollectServiceEntry.IChatCollectServiceEntry TargetChatCollectServiceEntry { get; set; }
        /// <summary>
        /// ダイアログの結果として得た<see cref="Model.IChatCollectServiceCreationOption"/>
        /// </summary>
        public Model.ChatCollectServiceEntry.IChatCollectServiceCreationOption ChatCollectServiceCreationOption { get; set; }
    }
}
