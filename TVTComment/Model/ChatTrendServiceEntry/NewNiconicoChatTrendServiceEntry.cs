using System.Threading.Tasks;
using TVTComment.Model.ChatTrendService;

namespace TVTComment.Model
{
    class NewNiconicoChatTrendServiceEntry:IChatTrendServiceEntry
    {
        public string Name => "新ニコニコ実況";
        public string Description => "ニコニコ実況の勢いを表示します";

        private NiconicoUtils.LiveIdResolver liveIdResolver;

        public NewNiconicoChatTrendServiceEntry(NiconicoUtils.LiveIdResolver liveIdResolver)
        {
            this.liveIdResolver = liveIdResolver;
        }

        public Task<IChatTrendService> GetNewService()
        {
            return Task.FromResult((IChatTrendService)new NewNiconicoChatTrendService(liveIdResolver));
        }
    }
}
