using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.IPC.IPCMessage
{
    /// <summary>
    /// IPCMessageでデコードに無効な文字列が渡されたときに投げる例外
    /// </summary>
    class IPCMessageDecodeException : ApplicationException
    {
        public IPCMessageDecodeException()
        {
        }
        public IPCMessageDecodeException(string message) : base(message)
        {
        }
        protected IPCMessageDecodeException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
        public IPCMessageDecodeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    interface IIPCMessage
    {
        string MessageName { get; }
        
        
        /// <summary>
        /// エンコードされたContentを返す
        /// </summary>
        IEnumerable<string> Encode();

        /// <summary>
        /// Contentをデコードする
        /// </summary>
        void Decode(IEnumerable<string> content);
    }
}
