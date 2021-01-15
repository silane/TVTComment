using CoreTweet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.TwitterUtils
{
    class ChatTwitterStatusToChat
    {
        public static Chat Convert(Status status)
        {
            return new Chat(status.CreatedAt.DateTime ,status.Text, Chat.PositionType.Normal, Chat.SizeType.Normal, Color.FromArgb(255, 255, 255),status.User.ScreenName,(int)status.Id);
        }
    }
}
