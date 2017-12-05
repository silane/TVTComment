using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.NichanUtils
{
    /// <summary>
    /// 自動選択するスレッドを表現するオブジェクト
    /// </summary>
    class MatchingThread
    {
        public string BoardName { get; private set; }
        public Uri BoardUri { get; private set; }
        public string[] ThreadTitleKeywords { get; private set; }

        public MatchingThread(string boardName, Uri boardUri, string[] threadTitleKeywords)
        {
            BoardName = boardName;
            BoardUri = boardUri;
            ThreadTitleKeywords = threadTitleKeywords;
        }
    }
}
