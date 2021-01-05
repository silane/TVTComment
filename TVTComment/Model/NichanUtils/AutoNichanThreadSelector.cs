using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.NichanUtils
{
    class AutoNichanThreadSelector : INichanThreadSelector
    {
        private ThreadResolver threadResolver;

        public AutoNichanThreadSelector(ThreadResolver threadResolver)
        {
            this.threadResolver = threadResolver;
        }

        public async Task<IEnumerable<string>> Get(ChannelInfo channel, DateTime time)
        {
            MatchingThread matchingThread = threadResolver.Resolve(channel);
            if (matchingThread == null)
                return new string[0];

            IEnumerable<string> keywords = matchingThread.ThreadTitleKeywords.Select(x => x.ToLower().Normalize(NormalizationForm.FormKD));

            Nichan.Board board = Nichan.BoardParser.ParseFromUri(matchingThread.BoardUri.ToString());

            return board.Threads.Select(x => { x.Title = x.Title.ToLower().Normalize(NormalizationForm.FormKD); return x; })
                .Where(x =>x.ResCount<=1000 &&  keywords.All(keyword => x.Title.Contains(keyword))).OrderByDescending(x=>x.ResCount).Take(3).Select(x=>x.Uri.ToString());
        }
    }
}
