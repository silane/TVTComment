using System;
using System.Collections.Generic;
using System.Text;

namespace TVTComment.Model.NichanUtils
{
    class AutoNichanBoardSelector : INichanBoardSelector
    {
        public AutoNichanBoardSelector(ThreadResolver threadResolver)
        {
            this.threadResolver = threadResolver;
        }

        public string Get(ChannelInfo channel, DateTime time)
        {
            MatchingThread matchingThread = this.threadResolver.Resolve(channel.NetworkId, channel.ServiceId);
            if (matchingThread == null)
                return "";

            return matchingThread.BoardUri.ToString();
        }

        private readonly ThreadResolver threadResolver;
    }
}
