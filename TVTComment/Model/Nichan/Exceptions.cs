using System;

namespace Nichan
{
    /// <summary>
    /// 基底例外
    /// </summary>
    public class NichanException : Exception
    {
        public NichanException() { }
        public NichanException(string message) : base(message) { }
        public NichanException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// 通信に関する例外
    /// </summary>
    public class CommunicationException : NichanException
    {
        public string Url { get; }
        public string Details { get; }
        public CommunicationException(string url) : this(url, null, null)
        {
        }
        public CommunicationException(string url, string details, Exception inner) : base(
            $"Communication error on \"{url}\"" + (details == null ? "" : $" ({details})"), inner
        )
        {
            this.Url = url;
            this.Details = details;
        }
    }

    /// <summary>
    /// 通信できなかった場合の例外
    /// </summary>
    public class NetworkException : CommunicationException
    {
        public NetworkException(string url) : base(url) { }
        public NetworkException(string url, string details, Exception inner) : base(url, details, inner) { }
    }

    /// <summary>
    /// 通信した結果の応答に関する例外
    /// </summary>
    public class ResponseException : CommunicationException
    {
        public string Response { get; }
        public ResponseException(string url) : base(url) { }
        public ResponseException(string response, string url, string details, Exception inner) : base(url, details, inner)
        {
            this.Response = response;
        }
    }

    /// <summary>
    /// 応答の内容がおかしい場合の例外。JSONのはずなのにそうなっていない等。
    /// </summary>
    public class InvalidResponseException : ResponseException
    {
        public InvalidResponseException(string response, string url, string details, Exception inner) : base(response, url, details, inner) { }
    }

    /// <summary>
    /// エラー応答が返された場合の例外
    /// </summary>
    public class ErrorResponseException : ResponseException
    {
        public ErrorResponseException(string response, string url, string details, Exception inner) : base(response, url, details, inner) { }
    }

    /// <summary>
    /// HTTPステータスコードでエラー応答が返された場合の例外
    /// </summary>
    public class HttpErrorResponseException : ErrorResponseException
    {
        public int HttpStatusCode { get; }
        public HttpErrorResponseException(int httpStatusCode, string response, string url, string details, Exception inner) : base(response, url, details, inner)
        {
            this.HttpStatusCode = httpStatusCode;
        }
    }
}
