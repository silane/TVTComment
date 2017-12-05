using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Security;

namespace TVTComment.Model
{
    class NiconicoChatTrendService : IChatTrendService
    {
        public string Name => "ニコニコ実況";
        public TimeSpan UpdateInterval => new TimeSpan(0, 0, 0, 50);

        private NiconicoUtils.JkIdResolver jkIdResolver;
        private static HttpClient httpClient = new HttpClient();

        public NiconicoChatTrendService(NiconicoUtils.JkIdResolver jkIdResolver)
        {
            this.jkIdResolver = jkIdResolver;
        }
                
        public async Task<IForceValueData> GetForceValueData()
        {
            XDocument doc;
            try
            {
                Stream stream = await httpClient.GetStreamAsync(@"http://jk.nicovideo.jp/api/v2_app/getchannels");
                doc = XDocument.Load(stream);
            }
            catch(HttpRequestException e)
            {
                throw new ChatTrendServiceException("勢い値データのタウンロードに失敗しました",e);
            }
            return new NiconicoForceValueData(doc,jkIdResolver);
        }

        public void Dispose()
        {
            httpClient.CancelPendingRequests();
        }
    }
}
