using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TVTComment.Model.NichanUtils
{
    class FuzzyNichanThreadSelector : INichanThreadSelector
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private readonly string boardHost;
        private readonly string boardName;

        private static int getLevenshteinDistance(string str1,string str2)
        {
            int[,] d = new int[str1.Length+1, str2.Length+1];
            for (int i = 0; i <= str1.Length; i++)
                d[i, 0] = i;
            for (int i = 0; i <= str2.Length; i++)
                d[0, i] = i;

            for(int i=0;i<str1.Length;i++)
                for(int j=0;j<str2.Length;j++)
                {
                    int cost = str1[i] == str2[j] ? 0 : 1;
                    d[i+1, j+1] = new int[3] { d[i , j+1] + 1, d[i+1, j] + 1, d[i, j] + cost }.Min();
                }
            return d[str1.Length, str2.Length];
        }

        private static readonly Regex reBracket = new Regex(@"(【.*】)");
        private static string normalizeThreadTitle(string title)
        {
            title=reBracket.Replace(title, "");
            title=title.Replace("無断転載禁止", "");
            title=title.Replace("©", "");
            title=title.Replace("2ch.net", "");
            title=title.Replace("&#169;", "");
            return title;
        }

        public string BoardUri { get; }
        public string ThreadTitleExample { get; }
        public FuzzyNichanThreadSelector(string boardUri, string threadTitleExample)
        {
            var uri = new Uri(boardUri);
            this.boardHost = $"{uri.Scheme}://{uri.Host}";
            this.boardName = uri.Segments[1];
            if (this.boardName.EndsWith('/'))
                this.boardName = this.boardName[..^1];

            this.BoardUri = boardUri;
            ThreadTitleExample = normalizeThreadTitle(threadTitleExample);
        }

        public async Task<IEnumerable<string>> Get(ChannelInfo channel, DateTimeOffset time)
        {
            byte[] subjectBytes = await httpClient.GetByteArrayAsync($"{this.boardHost}/{this.boardName}/subject.txt");
            string subject = Encoding.GetEncoding(932).GetString(subjectBytes);

            using var textReader = new StringReader(subject);
            IEnumerable<Nichan.Thread> threadsInBoard = await Nichan.SubjecttxtParser.ParseFromStream(textReader);

            var threads = threadsInBoard.Where(x => x.ResCount <= 1000).
                Select(x => new { Thread = x, EditDistance = getLevenshteinDistance(normalizeThreadTitle(x.Title), ThreadTitleExample) }).
                OrderBy(x => x.EditDistance).ToArray();

            //レーベンシュタイン距離の最小値から距離10以内のものを選択
            return threads.TakeWhile(
                x => x.EditDistance <= threads.First().EditDistance + 10
            ).Take(3).Select(
                x => $"{this.boardHost}/test/read.cgi/{this.boardName}/{x.Thread.Name}/l50"
            );
        }
    }
}
