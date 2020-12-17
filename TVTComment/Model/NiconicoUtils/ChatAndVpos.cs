namespace TVTComment.Model.NiconicoUtils
{
    class ChatAndVpos
    {
        public Chat Chat { get; }

        public int Vpos { get; }

        public ChatAndVpos(Chat chat, int vpos)
        {
            Chat = chat;
            Vpos = vpos;
        }
    }

}
