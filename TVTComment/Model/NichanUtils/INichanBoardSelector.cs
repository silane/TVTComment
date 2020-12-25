using System;

namespace TVTComment.Model.NichanUtils
{
    interface INichanBoardSelector
    {
        string Get(ChannelInfo channel, DateTime time);
    }
}
