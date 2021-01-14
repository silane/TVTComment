using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.NichanUtils
{
    class KeywordNichanThreadSelector:INichanThreadSelector
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private readonly string boardHost;
        private readonly string boardName;

        public string BoardUri { get; }
        public IEnumerable<string> Keywords { get; }

        public KeywordNichanThreadSelector(string boardUri, IEnumerable<string> keywords)
        {
            var uri = new Uri(boardUri);
            this.boardHost = $"{uri.Scheme}://{uri.Host}";
            this.boardName = uri.Segments[1];
            if (this.boardName.EndsWith('/'))
                this.boardName = this.boardName[..^1];

            this.BoardUri = boardUri;
            Keywords = keywords.Select(x=>x.ToLower().Normalize(NormalizationForm.FormKD));
        }

        public async Task<IEnumerable<string>> Get(ChannelInfo channel, DateTime time)
        {
            byte[] subjectBytes = await httpClient.GetByteArrayAsync($"{this.boardHost}/{this.boardName}/subject.txt");
            string subject = Encoding.GetEncoding(932).GetString(subjectBytes);

            using var textReader = new StringReader(subject);
            IEnumerable<Nichan.Thread> threadsInBoard = await Nichan.SubjecttxtParser.ParseFromStream(textReader);

            return threadsInBoard.Select(
                x=> { x.Title = x.Title.ToLower().Normalize(NormalizationForm.FormKD); return x; }
            ).Where(
                x => Keywords.All(keyword => x.Title.Contains(keyword))
            ).Select(
                x => $"{this.boardHost}/test/read.cgi/{this.boardName}/{x.Name}/l50"
            );
        }
    }
}
