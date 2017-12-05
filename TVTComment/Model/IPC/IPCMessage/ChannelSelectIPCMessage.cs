using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.IPC.IPCMessage
{
    class ChannelSelectIPCMessage:IIPCMessage
    {
        public int SpaceIndex;
        public int ChannelIndex;
        public ushort ServiceId;

        public string MessageName => "ChannelSelect";

        public IEnumerable<string> Encode()
        {
            return new string[3] { SpaceIndex.ToString(), ChannelIndex.ToString(), ServiceId.ToString() };
        }

        public void Decode(IEnumerable<string> content)
        {
            throw new NotImplementedException("ChannelSelectIPCMessage::Decodeは実装されていません");
        }
    }
}
