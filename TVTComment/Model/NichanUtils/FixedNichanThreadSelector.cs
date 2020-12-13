using System;
using System.Collections.Generic;

namespace TVTComment.Model.NichanUtils
{
    class FixedNichanThreadSelector : INichanThreadSelector
    {
        public IEnumerable<string> Uris { get; }

        public FixedNichanThreadSelector(IEnumerable<string> uris)
        {
            this.Uris = uris;
        }

        public IEnumerable<string> Get(ChannelInfo channel, DateTime time)
        {
            return this.Uris;
        }
    }
}
