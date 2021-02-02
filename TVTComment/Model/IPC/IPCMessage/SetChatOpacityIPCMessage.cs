using System;
using System.Collections.Generic;

namespace TVTComment.Model.IPC.IPCMessage
{
    class SetChatOpacityIPCMessage : IIPCMessage
    {
        public string MessageName => "SetChatOpacity";

        public byte Opacity;

        public IEnumerable<string> Encode()
        {
            return new string[] { Opacity.ToString() };
        }

        public void Decode(IEnumerable<string> content)
        {
            throw new NotImplementedException("SetChatOpacity::Decode is not implemented");
        }
    }
}
