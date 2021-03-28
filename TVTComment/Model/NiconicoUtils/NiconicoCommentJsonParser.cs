using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace TVTComment.Model.NiconicoUtils
{
    class NiconicoCommentJsonParser
    {
        private bool socketFormat;
        private Queue<NiconicoCommentXmlTag> chats = new Queue<NiconicoCommentXmlTag>();
        private string buffer;

        /// <summary>
        /// <see cref="NiconicoCommentXmlParser"/>を初期化する
        /// </summary>
        /// <param name="socketFormat">ソケットを使うリアルタイムのデータ形式ならtrue 過去ログなどのデータ形式ならfalse</param>
        public NiconicoCommentJsonParser(bool socketFormat)
        {
            this.socketFormat = socketFormat;
        }

        public void Push(string str)
        {
            if (socketFormat)
            {
               if (str.StartsWith("{\"chat"))
               {
                   chats.Enqueue(getChatJSONTag(str));
               }
            }
            else
            {
                // サポートしない,
            }
        }

        /// <summary>
        /// 解析結果を返す <see cref="socketFormat"/>がfalseなら<see cref="ChatNiconicoCommentXmlTag"/>しか返さない
        /// </summary>
        /// <returns>解析結果の<see cref="NiconicoCommentXmlTag"/></returns>
        public NiconicoCommentXmlTag Pop()
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
            buffer = string.Empty;
            chats.Clear();
        }

        private static ChatNiconicoCommentXmlTag getChatJSONTag(string str) {
            JObject jsonObj = JObject.Parse(str);
            // {"chat":{"thread":"M.kk-tzPBrneGMsNfDNO1skg","no":45929,"vpos":6496293,"date":1616936565,"date_usec":664450,"mail":"184","user_id":"EvrCRqk2e04B-pYS7q44kVU5HR4","anonymity":1,"content":"結局SBの勝ちかいｗ"}}
            int vpos = int.Parse(jsonObj["chat"]["vpos"].ToString());
            long date = long.Parse(jsonObj["chat"]["date"].ToString());
            int dateUsec = jsonObj["chat"]["date_usec"] == null ? 0 : int.Parse(jsonObj["chat"]["date_usec"].ToString());
            string mail = jsonObj["chat"]["mail"] == null ? "" : jsonObj["chat"]["mail"].ToString();
            string userId = jsonObj["chat"]["user_id"].ToString();
            int premium = jsonObj["chat"]["premium"] == null ? 0 : int.Parse(jsonObj["chat"]["premium"].ToString());
            int anonymity = jsonObj["chat"]["anonymity"] == null ? 0 : int.Parse(jsonObj["chat"]["anonymity"].ToString());
            int abone = jsonObj["chat"]["abone"] == null ? 0 : int.Parse(jsonObj["chat"]["abone"].ToString());
            string content = (string)jsonObj["chat"]["content"];
            int no = int.Parse(jsonObj["chat"]["no"].ToString());
            return new ChatNiconicoCommentXmlTag(
                        content, 0, no, vpos, date, dateUsec, mail, userId, premium, anonymity, abone
                    );
        }
    }
}
