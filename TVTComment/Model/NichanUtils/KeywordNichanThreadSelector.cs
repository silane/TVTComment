using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.NichanUtils
{
    class KeywordNichanThreadSelector:INichanThreadSelector
    {
        public Uri BoardUri { get; }
        public IEnumerable<string> Keywords { get; }

        public KeywordNichanThreadSelector(Uri boardUri,IEnumerable<string> keywords)
        {
            BoardUri = boardUri;
            Keywords = keywords.Select(x=>x.ToLower().Normalize(NormalizationForm.FormKD));
        }

        public IEnumerable<string> Get(ChannelInfo channel, DateTime time)
        {
            Nichan.Board board = Nichan.BoardParser.ParseFromUri(BoardUri.ToString());

            return board.Threads.Select(x=> { x.Title = x.Title.ToLower().Normalize(NormalizationForm.FormKD); return x; })
                .Where(x => Keywords.All(keyword => x.Title.Contains(keyword))).Select(x => x.Uri.ToString());
        }
    }
}
