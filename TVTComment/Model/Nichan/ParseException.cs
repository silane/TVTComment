using System;

namespace Nichan
{
    [Serializable]
    class InternalParseException : Exception
    {
        /// <summary>
        /// <see cref="ParseException"/>に伝達するメッセージ
        /// </summary>
        public string TransferedMessage { get; } = null;
        public InternalParseException() { }
        public InternalParseException(string transferedMessage) { TransferedMessage = transferedMessage; }
        public InternalParseException(string transferedMessage, Exception inner) : base(null, inner) { TransferedMessage = transferedMessage; }
        protected InternalParseException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class ParseException : Exception
    {
        internal ParseException(Uri source) : this(source, (string)null) { }
        internal ParseException(Uri source, string message) : this(source, message, null) { }
        internal ParseException(Uri source, string message, Exception inner) : base($@"Could not parse ""{source}"". {message}", inner) { }
        internal ParseException(Uri source, InternalParseException e) : this(source, e.TransferedMessage ?? "", e) { }
        protected ParseException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}