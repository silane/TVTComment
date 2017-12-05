using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.NichanUtils
{
    interface INichanThreadSelector
    {
        IEnumerable<string> Get(ChannelInfo channel, DateTime time);
    }
}
