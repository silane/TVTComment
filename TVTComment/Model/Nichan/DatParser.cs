// 参考: http://info.5ch.net/index.php/Monazilla/develop/dat

using Sgml;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;


namespace Nichan
{

    [Serializable]
    public class DatParserException : Exception
    {
        public DatParserException() { }
        public DatParserException(string message) : base(message) { }
        public DatParserException(string message, Exception inner) : base(message, inner) { }
        protected DatParserException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public static class DatParser
    {
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

        public static Thread Parse(string dat)
        {
            var thread = new Thread();

            var rows = dat.Split(
                '\n', StringSplitOptions.RemoveEmptyEntries
            ).Select(row => row.Split("<>")).ToArray();

            foreach(var row in rows)
            {
                if (row.Length != 5)
                    throw new DatParserException();
            }

            thread.Title = rows[0][4];
            thread.Res.AddRange(rows.Select((row, idx) => {
                int startIdx = row[2].IndexOf("ID:");
                string userId = startIdx != -1 ? row[2].Substring(startIdx + 3) : null;


                //string text = string.Join('\n', row[3].Split('<br>').Select(x => {
                //    if (x.Length == 0)
                //        return x;
                //    if (x[0] == ' ')
                //        x = x.Substring(1);

                //    if (x.Length == 0)
                //        return x;
                //    if (x[x.Length - 1] == ' ')
                //        x = x.Substring(0, x.Length - 1);
                //    return x;
                //}).Select(x => HttpUtility.HtmlDecode(x)));
                XElement text;
                using (StringReader reader = new StringReader($"<html><div>{row[3]}</div></html>"))
                {
                    using (var sgml = new SgmlReader { DocType = "HTML", IgnoreDtd = false, InputStream = reader })
                    {
                        sgml.WhitespaceHandling = WhitespaceHandling.All;
                        sgml.CaseFolding = Sgml.CaseFolding.ToLower;
                        text = XDocument.Load(sgml).XPathSelectElement("./html/div");
                    }
                }

                return new Res
                {
                    Number = idx + 1,
                    Name = HttpUtility.HtmlDecode(row[0]),
                    Mail = HttpUtility.HtmlDecode(row[1]),
                    UserId = userId,
                    Date = getDate(row[2]),
                    Text = text,
                };
            }));
            thread.ResCount = thread.Res.Count;

            return thread;
        }
    }
}
