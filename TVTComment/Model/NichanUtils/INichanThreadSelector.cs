using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TVTComment.Model.NichanUtils
{
    interface INichanThreadSelector
    {
        Task<IEnumerable<string>> Get(
            ChannelInfo channel, DateTimeOffset time, CancellationToken cancellationToken
        );
    }
}
