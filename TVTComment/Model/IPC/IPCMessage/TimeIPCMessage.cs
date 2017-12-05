using System;
using System.Collections.Generic;
using System.Linq;

namespace TVTComment.Model.IPC.IPCMessage
{
    /// <summary>
    /// 現在再生中の番組の時刻を伝える
    /// 精度は1秒単位で値が変わるごとに来る
    /// </summary>
    class TimeIPCMessage : IIPCMessage
    {
        /// <summary>
        /// 時刻(JST)
        /// </summary>
        public DateTime Time;

        public string MessageName => "Time";
       
        public void Decode(IEnumerable<string> content)
        {
            try
            {
                Time = DateTimeOffset.FromUnixTimeSeconds(long.Parse(content.First())).DateTime.ToLocalTime();
            }
            catch(FormatException e)
            {
                throw new IPCMessageDecodeException("Timeのフォーマットが不正です", e);
            }
            catch(InvalidOperationException e)
            {
                throw new IPCMessageDecodeException("Timeのcontentの数が0です", e);
            }
        }

        public IEnumerable<string> Encode()
        {
            throw new NotImplementedException("TimeIPCMessage::Encodeは実装されていません");
        }
    }
}
