using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;

namespace TVTComment.Model.NiconicoUtils
{
    /// <summary>
    /// <see cref="NiconicoLoginSession"/>で投げられる例外
    /// </summary>
    [System.Serializable]
    class NiconicoLoginSessionException : Exception
    {
        public NiconicoLoginSessionException() { }
        public NiconicoLoginSessionException(string message) : base(message) { }
        public NiconicoLoginSessionException(string message, Exception inner) : base(message, inner) { }
        protected NiconicoLoginSessionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// ログインに失敗した
    /// </summary>
    class LoginFailureNiconicoLoginSessionException : NiconicoLoginSessionException
    { }

    /// <summary>
    /// ネットワークエラーが発生した
    /// </summary>
    class NetworkNiconicoLoginSessionException : NiconicoLoginSessionException
    {
        public NetworkNiconicoLoginSessionException(Exception inner) : base(null, inner)
        { }
    }

    class NiconicoLoginSession
    {
        private string mail;
        private string password;
        private CookieCollection cookie = null;

        public bool IsLoggedin => cookie != null;
        /// <summary>
        /// 送信するべき認証情報を含んだクッキー
        /// </summary>
        /// <exception cref="InvalidOperationException">ログインしていない</exception>
        public CookieCollection Cookie
        {
            get
            {
                if (this.IsLoggedin)
                    return this.cookie;
                else
                    throw new InvalidOperationException("ログインしていません");
            }
        }

        public NiconicoLoginSession(string mail, string password)
        {
            this.mail = mail;
            this.password = password;
        }

        /// <summary>
        /// ログインする
        /// </summary>
        /// <exception cref="InvalidOperationException">すでにログインしている</exception>
        /// <exception cref="LoginFailureNiconicoLoginSessionException"></exception>
        /// <exception cref="NetworkNiconicoLoginSessionException"></exception>
        public async Task Login()
        {
            if (this.IsLoggedin)
                throw new InvalidOperationException("すでにログインしています");

            const string loginUrl = "https://secure.nicovideo.jp/secure/login?site=niconico";

            var handler = new HttpClientHandler();
            using var client = new HttpClient(handler);

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "next_url", "" },
                { "mail", this.mail },
                { "password", this.password }
            });
            
            try
            {
                await client.PostAsync(loginUrl, content).ConfigureAwait(false);
            }
            catch(HttpRequestException e)
            {
                throw new NetworkNiconicoLoginSessionException(e);
            }

            CookieCollection cookieCollection = handler.CookieContainer.GetCookies(new Uri(loginUrl));
            if (cookieCollection.All(x => x.Name != "user_session"))
                throw new LoginFailureNiconicoLoginSessionException();

            this.cookie = cookieCollection;
        }

        /// <summary>
        /// ログアウトする
        /// </summary>
        /// <exception cref="InvalidOperationException">ログインしていない</exception>
        /// <exception cref="NetworkNiconicoLoginSessionException"></exception>
        public async Task Logout()
        {
            if (!this.IsLoggedin)
                throw new InvalidOperationException("ログインしていません");

            var handler = new HttpClientHandler();
            using var client = new HttpClient(handler);

            handler.CookieContainer.Add(this.cookie);
            try
            {
                await client.GetAsync("https://secure.nicovideo.jp/secure/logout").ConfigureAwait(false);
            }
            catch(HttpRequestException e)
            {
                throw new NetworkNiconicoLoginSessionException(e);
            }
            this.cookie = null;
        }
    }
}
