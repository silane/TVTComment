using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TVTComment.Model.NichanUtils
{
    class AutoNichanThreadSelector : INichanThreadSelector
    {
        private static readonly HttpClient httpClient = new HttpClient();

        private readonly ThreadResolver threadResolver;

        public AutoNichanThreadSelector(ThreadResolver threadResolver)
        {
            this.threadResolver = threadResolver;
        }

        public async Task<IEnumerable<string>> Get(
            ChannelInfo channel, DateTimeOffset time, CancellationToken cancellationToken
        )
        {
            MatchingThread matchingThread = threadResolver.Resolve(channel, false);
            if (matchingThread == null)
                return new string[0];

            IEnumerable<string> keywords = matchingThread.ThreadTitleKeywords.Select(
                x => x.ToLower().Normalize(NormalizationForm.FormKD)
            );

            string boardUri = matchingThread.BoardUri.ToString();
            var uri = new Uri(boardUri);
            string boardHost = $"{uri.Scheme}://{uri.Host}";
            string boardName = uri.Segments[1];
            if (boardName.EndsWith('/'))
                boardName = boardName[..^1];

            byte[] subjectBytes = await httpClient.GetByteArrayAsync(
                $"{boardHost}/{boardName}/subject.txt", cancellationToken
            );
            string subject = Encoding.GetEncoding(932).GetString(subjectBytes);

            using var textReader = new StringReader(subject);
            IEnumerable<Nichan.Thread> threadsInBoard = await Nichan.SubjecttxtParser.ParseFromStream(textReader);

            return threadsInBoard.Select(
                x => { x.Title = x.Title.ToLower().Normalize(NormalizationForm.FormKD); return x; }
            ).Where(
                x =>x.ResCount<=1000 && keywords.All(keyword => x.Title.Contains(keyword))
            ).OrderByDescending(x => x.ResCount).Take(3).Select(
                x => $"{boardHost}/test/read.cgi/{boardName}/{x.Name}/l50"
            );
        }
    }
}
