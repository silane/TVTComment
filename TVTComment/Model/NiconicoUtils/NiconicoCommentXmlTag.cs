using System;
using System.Collections.Generic;
using System.Text;

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

    class NiconicoCommentXmlTag
    {
        /// <summary>
        /// サーバーから取得する場合に受信した時刻を格納する(JST)
        /// </summary>
        public DateTime? ReceivedTime { get; }
        public NiconicoCommentXmlTag(DateTime? receivedTime = null)
        {
            ReceivedTime = receivedTime;
        }
    }
    class ChatNiconicoCommentXmlTag : NiconicoCommentXmlTag
    {
        public ChatAndVpos Chat { get; }
        public ChatNiconicoCommentXmlTag(ChatAndVpos chat)
        {
            Chat = chat;
        }
    }
    class ThreadNiconicoCommentXmlTag : NiconicoCommentXmlTag
    {
        /// <summary>
        /// スレッドができた日付時刻をUNIX時刻JSTで表現したもの
        /// </summary>
        public ulong Thread { get; }
        public string Ticket { get; }
        public ulong ServerTime { get; }
        public ThreadNiconicoCommentXmlTag(DateTime receivedTime, ulong thread, string ticket, ulong serverTime) : base(receivedTime)
        {
            Thread = thread;
            Ticket = ticket;
            ServerTime = serverTime;
        }
    }
    class ChatResultNiconicoCommentXmlTag : NiconicoCommentXmlTag
    {
        public int Status { get; }
        public ChatResultNiconicoCommentXmlTag(int status)
        {
            Status = status;
        }
    }
    class LeaveThreadNiconicoCommentXmlTag : NiconicoCommentXmlTag { }

}
