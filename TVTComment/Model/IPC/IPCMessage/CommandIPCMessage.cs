using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.IPC.IPCMessage
{
    class CommandIPCMessage:IIPCMessage
    {
        public string MessageName=>"Command";

        public string CommandId { get; private set; }

        public IEnumerable<string> Encode()
        {
            return new string[1] { CommandId };
        }

        public void Decode(IEnumerable<string> content)
        {
            string[] contents = content.ToArray();
            if (contents.Length != 1)
                throw new IPCMessageDecodeException("CurrentChannelのcontentの数が1以外です");

            CommandId = contents[0];
        }
    }
}
