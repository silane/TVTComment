using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace TVTComment.Model.NiconicoUtils
{
    class ChatAndVpos
    {
        public Chat Chat { get; }
        
        public int Vpos { get; }

        public ChatAndVpos(Chat chat,int vpos)
        {
            Chat = chat;
            Vpos = vpos;
        }
    }

    class NiconicoJikkyouXmlParser
    {
        public class XmlTag
        {
            /// <summary>
            /// 受信した時刻 正確にはパースされた時刻(JST)
            /// </summary>
            public DateTime? ReceivedTime { get; }
            public XmlTag(DateTime? receivedTime=null)
            {
                ReceivedTime = receivedTime;
            }
        }
        public class ChatXmlTag:XmlTag
        {
            public ChatAndVpos Chat { get; }
            public ChatXmlTag(ChatAndVpos chat)
            {
                Chat = chat;
            }
        }
        public class ThreadXmlTag : XmlTag
        {
            /// <summary>
            /// スレッドができた日付時刻をUNIX時刻JSTで表す
            /// </summary>
            public ulong Thread { get; }
            public string Ticket { get; }
            public ulong ServerTime { get; }
            public ThreadXmlTag(DateTime receivedTime,ulong thread,string ticket,ulong serverTime):base(receivedTime)
            {
                Thread = thread;
                Ticket = ticket;
                ServerTime = serverTime;
            }
        }
        public class ChatResultXmlTag : XmlTag
        {
            public int Status { get; }
            public ChatResultXmlTag(int status)
            {
                Status = status;
            }
        }
        public class LeaveThreadXmlTag : XmlTag { }

        private static readonly Dictionary<string, Color?> colorNameMapping = new Dictionary<string, Color?> {
            {"red" , Color.FromArgb(0xFF, 0x00, 0x00)},
            {"pink", Color.FromArgb(0xFF, 0x80, 0x80)},
            {"orange",Color.FromArgb(0xFF, 0xC0, 0x00)},
            {"yellow",Color.FromArgb(0xFF, 0xFF, 0x00)},
            {"green", Color.FromArgb(0x00, 0xFF, 0x00) },
            {"cyan", Color.FromArgb(0x00, 0xFF, 0xFF) },
            {"blue", Color.FromArgb(0x00, 0x00, 0xFF) },
            {"purple", Color.FromArgb(0xC0, 0x00, 0xFF)},
            {"black", Color.FromArgb(0x00, 0x00, 0x00) },
            {"white2", Color.FromArgb(0xCC, 0xCC, 0x99) },
            {"niconicowhite", Color.FromArgb(0xCC, 0xCC, 0x99) },
            {"red2", Color.FromArgb(0xCC, 0x00, 0x33) },
            {"truered", Color.FromArgb(0xCC, 0x00, 0x33) },
            {"pink2",Color.FromArgb(0xFF, 0x33, 0xCC)},
            {"orange2", Color.FromArgb(0xFF, 0x66, 0x00) },
            {"passionorange", Color.FromArgb(0xFF, 0x66, 0x00) },
            {"yellow2", Color.FromArgb(0x99, 0x99, 0x00) },
            {"madyellow", Color.FromArgb(0x99, 0x99, 0x00) },
            {"green2", Color.FromArgb(0x00, 0xCC, 0x66) },
            {"elementalgreen", Color.FromArgb(0x00, 0xCC, 0x66) },
            {"cyan2", Color.FromArgb(0x00, 0xCC, 0xCC) },
            {"blue2", Color.FromArgb(0x33, 0x99, 0xFF) },
            {"marineblue", Color.FromArgb(0x33, 0x99, 0xFF) },
            {"purple2", Color.FromArgb(0x66, 0x33, 0xCC)},
            {"nobleviolet", Color.FromArgb(0x66, 0x33, 0xCC) },
            {"black2", Color.FromArgb(0x66, 0x66, 0x66) },
        };

        private bool socketFormat;
        private bool inChatTag;
        private bool inThreadTag;
        private Queue<XmlTag> chats=new Queue<XmlTag>();
        private string buffer;

        /// <summary>
        /// <see cref="NiconicoJikkyouXmlParser"/>を初期化する
        /// </summary>
        /// <param name="socketFormat">ソケットを使うリアルタイムの実況データ形式ならtrue 過去ログなどのデータ形式ならfalse</param>
        public NiconicoJikkyouXmlParser(bool socketFormat)
        {
            this.socketFormat = socketFormat;
            this.inChatTag = false;
            this.inThreadTag = false;
        }

        public void Push(string str)
        {
            buffer += str;

            if(socketFormat)
            {
                string[] tmp = buffer.Split('\0');
                foreach(string tagStr in tmp.Take(tmp.Length-1))
                {
                    if (tagStr.StartsWith("<chat_result"))
                    {
                        int idx = tagStr.IndexOf("status=")+8;
                        int status = int.Parse(tagStr.Substring(idx, tagStr.IndexOf('"', idx) - idx));
                        chats.Enqueue(new ChatResultXmlTag(status));
                    }
                    else if (tagStr.StartsWith("<chat"))
                    {
                        chats.Enqueue(new ChatXmlTag(new ChatAndVpos(getChatFromChatTag(tagStr), getVposFromChatTag(tagStr))));
                    }
                    else if(tagStr.StartsWith("<thread"))
                    {
                        chats.Enqueue(getThreadXmlTag(tagStr));
                    }
                    else if(tagStr.StartsWith("<leave_thread"))
                    {
                        chats.Enqueue(new LeaveThreadXmlTag());
                    }
                }
                buffer = tmp[tmp.Length - 1];
            }
            else
            {
                while(true)
                {
                    if(inChatTag)
                    {
                        int idx = buffer.IndexOf("</chat>");
                        if (idx == -1) break;
                        idx += 7;
                        string tagStr = buffer.Substring(0, idx);
                        buffer = buffer.Substring(idx);
                        inChatTag = false;
                        chats.Enqueue(new ChatXmlTag(new ChatAndVpos(getChatFromChatTag(tagStr), getVposFromChatTag(tagStr))));
                    }
                    else if(inThreadTag)
                    {
                        int idx = buffer.IndexOf("/>");
                        if (idx == -1) break;
                        idx += 2;
                        string tagStr = buffer.Substring(0, idx);
                        buffer = buffer.Substring(idx);
                        inThreadTag = false;
                        chats.Enqueue(getThreadXmlTag(tagStr));
                    }
                    else
                    {
                        int idx = buffer.IndexOf('<');
                        if (idx == -1) break;

                        buffer = buffer.Substring(idx);
                        if (buffer.StartsWith("<chat"))
                            inChatTag = true;
                        else if (buffer.StartsWith("<thread"))
                            inThreadTag = true;
                        else
                        {
                            if (buffer.Length > 0)
                                buffer = buffer.Substring(1);
                            else
                                break;
                        }
                    }                   
                }
            }
        }

        /// <summary>
        /// 解析結果を返す <see cref="socketFormat"/>がfalseなら<see cref="ChatXmlTag"/>しか返さない
        /// </summary>
        /// <returns>解析結果の<see cref="XmlTag"/></returns>
        public XmlTag Pop()
        {
            return chats.Dequeue();
        }

        /// <summary>
        /// <see cref="Pop"/>で読みだすデータがあるか
        /// </summary>
        public bool DataAvailable()
        {
            return chats.Count > 0;
        }

        public void Reset()
        {
            inChatTag = false;
            inThreadTag = false;
            buffer = string.Empty;
            chats.Clear();
        }

        private static readonly Regex reChat=new Regex("<chat(?= )(.*)>(.*?)</chat>",RegexOptions.Singleline);
        private static readonly Regex reMail=new Regex(" mail=\"(.*?)\"");
        private static readonly Regex reAbone=new Regex(" abone=\"1\"");
        private static readonly Regex reUserID=new Regex(" user_id=\"([0-9A-Za-z\\-_]{0,27})");
        private static readonly Regex reColor=new Regex("(?:^| )#([0-9A-Fa-f]{6})(?: |$)");
        private static Chat getChatFromChatTag(string str)
        {
            string text, userId;
            Chat.PositionType position=Chat.PositionType.Normal;
            Chat.SizeType size=Chat.SizeType.Normal;
            Color color=Color.FromArgb(255,255,255);

            text=HttpUtility.HtmlDecode( reChat.Match(str).Groups[2].Value);
            userId = reUserID.Match(str).Groups[1].Value;

            var match = reMail.Match(str);
            if(match.Success)
            {
                string mail = HttpUtility.HtmlDecode( match.Groups[1].Value);

                if (mail.Contains("shita"))
                    position = Chat.PositionType.Bottom;
                else if (mail.Contains("ue"))
                    position = Chat.PositionType.Top;
                else
                    position = Chat.PositionType.Normal;

                if (mail.Contains("small"))
                    size = Chat.SizeType.Small;
                else if (mail.Contains("big"))
                    size = Chat.SizeType.Large;
                else
                    size = Chat.SizeType.Normal;

                match = reColor.Match(mail);
                if(match.Success)
                {
                    int colorNum = int.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
                    color = Color.FromArgb((colorNum >> 16) & 0xFF, (colorNum >> 8) & 0xFF, colorNum & 0xFF);
                }
                else
                {
                    color = colorNameMapping.FirstOrDefault((kv) => mail.Contains(kv.Key)).Value ?? Color.FromArgb(255, 255, 255);
                }
            }

            return new Chat(getTimeFromChatTag(str), text, position, size, color, userId,getNumberFromChatTag(str));
        }

        private static readonly Regex reDate=new Regex("date=\"(\\d+)\"");
        private static DateTime getTimeFromChatTag(string str)
        {
            return DateTimeOffset.FromUnixTimeSeconds(long.Parse(reDate.Match(str).Groups[1].Value)).DateTime.ToLocalTime();
        }

        private static readonly Regex reNo = new Regex(" no=\"(\\d+)\"");
        private static int getNumberFromChatTag(string str)
        {
            return int.Parse(reNo.Match(str).Groups[1].Value);
        }

        private static readonly Regex reVpos = new Regex(@"vpos=""(-?\d+)""");
        private static int getVposFromChatTag(string str)
        {
            return int.Parse(reVpos.Match(str).Groups[1].Value);
        }

        private static ThreadXmlTag getThreadXmlTag(string tagStr)
        {
            int idx = tagStr.IndexOf("thread=") + 8;
            ulong thread = ulong.Parse(tagStr.Substring(idx, tagStr.IndexOf('"', idx) - idx));
            idx = tagStr.IndexOf("ticket=") + 8;
            string ticket = tagStr.Substring(idx, tagStr.IndexOf('"', idx) - idx);
            idx = tagStr.IndexOf("server_time=") + 13;
            ulong serverTime = ulong.Parse(tagStr.Substring(idx, tagStr.IndexOf('"', idx) - idx));
            return new ThreadXmlTag(getDateTimeJstNow(), thread, ticket, serverTime);
        }
        

        /// <summary>
        /// (マシンのカルチャ設定に関係なく)今の日本標準時を返す
        /// </summary>
        private static DateTime getDateTimeJstNow()
        {
            return DateTime.SpecifyKind(DateTime.UtcNow.AddHours(9),DateTimeKind.Unspecified);
        }
    }
}
