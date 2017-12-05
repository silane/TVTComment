using ObservableUtils;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TVTComment.Model
{
    class NiconicoChatTrendServiceEntry:IChatTrendServiceEntry
    {
        public string Name => "ニコニコ実況";
        public string Description => "ニコニコ実況の勢いを表示します";

        private NiconicoUtils.JkIdResolver jkIdResolver;

        public NiconicoChatTrendServiceEntry(NiconicoUtils.JkIdResolver jkIdResolver)
        {
            this.jkIdResolver = jkIdResolver;
        }

        public Task<IChatTrendService> GetNewService()
        {
            return Task.FromResult((IChatTrendService)new NiconicoChatTrendService(jkIdResolver));
        }
    }
}
