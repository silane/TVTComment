using System.Collections.Generic;
using TVTComment.Model.NiconicoUtils;

namespace TVTComment.Model.ChatTrendService
{
    class NewNiconicoForceValueData : IForceValueData
    {
        private readonly LiveIdResolver liveIdResolver;
        private readonly Dictionary<string, int[]> forces;

        public NewNiconicoForceValueData(Dictionary<string, int[]> forces, LiveIdResolver liveIdResolver)
        {
            this.liveIdResolver = liveIdResolver;
            this.forces = forces;
        }

        public int? GetForceValue(ChannelInfo channelInfo)
        {
            var liveId = liveIdResolver.Resolve(channelInfo.NetworkId, channelInfo.ServiceId);
            if (liveId.Equals("") || !forces.ContainsKey(liveId))
                return null;

            var live = forces[liveId];
            var oldCount = live[0];
            var nowCount = live[1];
            return nowCount - oldCount;
        }
    }
}
