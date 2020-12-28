// 参考: http://info.5ch.net/index.php/Monazilla/develop/dat

using Sgml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;


namespace Nichan
{
    public class DatParserException : NichanException
    {
        public DatParserException() { }
        public DatParserException(string message) : base(message) { }
        public DatParserException(string message, Exception inner) : base(message, inner) { }
    }

    public class DatParser
    {
        public bool FromTheMiddle { get; }
        /// <summary>
        /// 解析結果のスレタイが入る。解析前の場合は<c>null</c>。
        /// </summary>
        public string ThreadTitle { get; private set; }

        public DatParser() : this(false)
        {
        }

        /// <param name="fromTheMiddle">
        /// 真の場合、datデータを最初からでなく中途半端な位置から与えられても解析する。その場合スレタイの解析は行われないしレス番号は正しくない。
        /// </param>
        public DatParser(bool fromTheMiddle)
        {
            this.FromTheMiddle = fromTheMiddle;
            this.Reset();
        }

        /// <summary>
        /// DAT文字列を追加し解析する
        /// </summary>
        /// <exception cref="DatParserException"></exception>
        public void Feed(string datString)
        {
            this.buffer += datString;
            if (this.buffer == "")
                return;
            var rows = this.buffer.Split('\n');
            var reses = rows[..^1].Select(x => x.Split("<>")).ToArray();
            this.buffer = rows[^1];

            bool isFirstLine = this.isBeforeFirstLine && reses.Length > 0; // 真ならreses[0]は最初の行
            if(isFirstLine)
            {
                this.isBeforeFirstLine = false;
            }

            if (isFirstLine && this.FromTheMiddle)
            {
                // 中途半端な位置から始まるデータの場合、最初の行は省く
                reses = reses[1..];
            }

            if (reses.Any(x => x.Length != 5))
            {
                throw new DatParserException();
            }

            if (isFirstLine && !this.FromTheMiddle)
            {
                // 最初の行からスレタイを取得
                this.ThreadTitle = reses[0][4];
            }

            using var sgmlReader = new SgmlReader() { DocType = "HTML", IgnoreDtd = false };
            sgmlReader.WhitespaceHandling = WhitespaceHandling.All;
            sgmlReader.CaseFolding = CaseFolding.ToLower;

            foreach (var row in reses)
            {
                int startIdx = row[2].IndexOf("ID:");
                string userId = startIdx != -1 ? row[2][(startIdx + 3)..] : null;

                using var reader = new StringReader($"<html><div>{row[3]}</div></html>");
                sgmlReader.InputStream = reader;
                XElement text = XDocument.Load(sgmlReader).XPathSelectElement("./html/div");

                this.reses.Enqueue(new Res
                {
                    Number = ++this.resNum,
                    Name = HttpUtility.HtmlDecode(row[0]),
                    Mail = HttpUtility.HtmlDecode(row[1]),
                    UserId = userId,
                    Date = getDate(row[2]),
                    Text = text,
                });
            }
        }

        /// <summary>
        /// 解析結果のレスのキューからレスをポップして返す
        /// </summary>
        public Res PopRes()
        {
            return this.reses.TryDequeue(out Res res) ? res : null;
        }

        /// <summary>
        /// パーサの状態を初期化する
        /// </summary>
        public void Reset()
        {
            this.ThreadTitle = null;
            this.buffer = "";
            this.reses.Clear();
            this.resNum = 0;
            this.isBeforeFirstLine = true;
        }

        private string buffer;
        private Queue<Res> reses = new Queue<Res>();
        private int resNum;
        private bool isBeforeFirstLine;

        private static readonly Regex reDate = new Regex(@"(\d+)/(\d+)/(\d+)[^ ]* (\d+):(\d+):(\d+)\.(\d+)");
        private static DateTime? getDate(string str)
        {
            Match m = reDate.Match(str);
            if (m.Success)
                return new DateTime(
                    int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value),
                    int.Parse(m.Groups[4].Value), int.Parse(m.Groups[5].Value), int.Parse(m.Groups[6].Value), int.Parse(m.Groups[7].Value) * 10, DateTimeKind.Local
                );
            else
                return null;
        }
    }
}
