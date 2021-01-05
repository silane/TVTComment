using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TVTComment.Model.NichanUtils
{
    interface INichanThreadSelector
    {
        Task<IEnumerable<string>> Get(ChannelInfo channel, DateTime time);
    }
}
