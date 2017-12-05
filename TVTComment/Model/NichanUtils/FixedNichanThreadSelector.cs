using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.NichanUtils
{
    class FixedNichanThreadSelector:INichanThreadSelector
    {
        private IEnumerable<string> uris;

        public FixedNichanThreadSelector(IEnumerable<string> uris)
        {
            this.uris = uris;
        }

        public IEnumerable<string> Get(ChannelInfo channel,DateTime time)
        {
            return uris;
        }
    }
}
