using CoreTweet;
using System.Drawing;

namespace TVTComment.Model.TwitterUtils
{
    class ChatTwitterStatusToChat
    {
        public static Chat Convert(Status status)
        {
            return new Chat(status.CreatedAt.DateTime.ToLocalTime() ,status.Text, Chat.PositionType.Normal, Chat.SizeType.Normal, Color.FromArgb(0, 172, 238),status.User.ScreenName,(int)status.Id);
        }
    }
}
