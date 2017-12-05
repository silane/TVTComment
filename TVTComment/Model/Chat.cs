using System;
using System.Drawing;

namespace TVTComment.Model
{
    public class Chat
    {
        public enum PositionType{ Normal,Top,Bottom}
        public enum SizeType { Normal,Small,Large}

        /// <summary>
        /// 投稿された日付時刻(JST)
        /// </summary>
        public DateTime Time { get; private set; }

        public string Text { get; private set; }
        public PositionType Position { get; private set; }
        public SizeType Size { get; private set; }
        public Color Color { get; private set; }
        public bool Ng { get; private set; }
        public string UserId { get; private set; }
        public int Number { get; private set; }

        public ChatCollectServiceEntry.IChatCollectServiceEntry SourceService { get; private set; }

        public Chat(DateTime time,string text,PositionType position,SizeType size,Color color,string userId,int number)
        {
            this.Time = time;
            this.Text = text;
            this.Position = position;
            this.Size = size;
            this.Color = color;
            this.UserId = userId;
            this.Number = number;
        }

        public void SetText(string text)
        {
            this.Text = text;
        }

        public void SetPosition(PositionType position)
        {
            this.Position = position;
        }

        public void SetSize(SizeType size)
        {
            this.Size = size;
        }

        public void SetColor(Color color)
        {
            this.Color = color;
        }

        public void SetNg(bool ng)
        {
            this.Ng = ng;
        }

        public void SetSourceService(ChatCollectServiceEntry.IChatCollectServiceEntry sourceService)
        {
            this.SourceService = sourceService;
        }
    }
}
