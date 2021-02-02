using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TVTComment.Model.NichanUtils
{
    class AutoPastNichanThreadSelector : INichanThreadSelector
    {
        public AutoPastNichanThreadSelector(ThreadResolver threadResolver, TimeSpan getTimeSpan, TimeSpan backTime)
        {
            this.threadResolver = threadResolver;
            this.getTimeSpan = getTimeSpan;
            this.backTime = backTime;
        }

        public async Task<IEnumerable<string>> Get(ChannelInfo channel, DateTimeOffset time, CancellationToken cancellationToken)
        {
            MatchingThread matchingThread = threadResolver.Resolve(channel, true);
            if (matchingThread == null)
                return Enumerable.Empty<string>();

            string[] keywords = matchingThread.ThreadTitleKeywords?.Select(
                x => x.ToLower().Normalize(NormalizationForm.FormKD)
            ).ToArray() ?? Array.Empty<string>();
            (string server, string board) = GetServerAndBoardFromBoardUrl(matchingThread.BoardUri.ToString());

            if (!pastThreadListerCache.TryGetValue(board, out var threadLister))
            {
                threadLister = new Nichan.PastThreadLister(board, server, backTime);
                await threadLister.Initialize(cancellationToken).ConfigureAwait(false);
                pastThreadListerCache.Add(board, threadLister);
            }

            DateTimeOffset startTime = time;
            IEnumerable<Nichan.Thread> threads = await threadLister.GetBetween(
                startTime, startTime + getTimeSpan, cancellationToken
            ).ConfigureAwait(false);

            return threads.Where(x => keywords.All(
                keyword => x.Title.ToLower().Normalize(NormalizationForm.FormKD).Contains(keyword)
            )).Select(x => x.Uri.ToString());
        }

        private readonly ThreadResolver threadResolver;
        private readonly TimeSpan getTimeSpan;
        private readonly TimeSpan backTime;
        private readonly Dictionary<string, Nichan.PastThreadLister> pastThreadListerCache = new Dictionary<string, Nichan.PastThreadLister>();

        private static readonly Regex reBoardUrl = new Regex(@"//(?<server>[^.]*)\.\dch\.(net|sc)/(?<board>[^/]*)(^|/.*)");
        private static (string server, string board) GetServerAndBoardFromBoardUrl(string boardUrl)
        {
            Match match = reBoardUrl.Match(boardUrl);
            return (match.Groups["server"].Value, match.Groups["board"].Value);
        }
    }
}
