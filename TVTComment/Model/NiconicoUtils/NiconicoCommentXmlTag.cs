using System;
using System.Collections.Generic;
using System.Text;

namespace TVTComment.Model.NiconicoUtils
{
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
        public string Text { get; }
        public long Thread { get; }
        public int No { get; }
        public int Vpos { get; }
        public long Date { get; }
        public int DateUsec { get; }
        public string Mail { get; }
        public string UserId { get; }
        public int Premium { get; }
        public int Anonymity { get; }
        public int Abone { get; }

        public ChatNiconicoCommentXmlTag(
            string text, long thread, int no, int vpos, long date, int dateUsec,
            string mail, string userId, int premium, int anonymity, int abone
        )
        {
            this.Text = text;
            this.Thread = thread;
            this.No = no;
            this.Vpos = vpos;
            this.Date = date;
            this.DateUsec = dateUsec;
            this.Mail = mail;
            this.UserId = userId;
            this.Premium = premium;
            this.Anonymity = anonymity;
            this.Abone = abone;
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
