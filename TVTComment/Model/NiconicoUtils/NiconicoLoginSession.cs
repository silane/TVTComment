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
        public string nicosid { get; private set; }
        public string session { get; private set; }
        public string secure { get; private set; }
        private CookieCollection cookie = null;

        public bool BadSession = false;
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

        public NiconicoLoginSession(string mail, string password, string nicosid, string session, string secure)
        {
            this.mail = mail;
            this.password = password;
            this.nicosid = nicosid;
            this.session = session;
            this.secure = secure;
        }

        /// <summary>
        /// ログインする
        /// </summary>
        /// <exception cref="InvalidOperationException">すでにログインしている</exception>
        /// <exception cref="LoginFailureNiconicoLoginSessionException"></exception>
        /// <exception cref="NetworkNiconicoLoginSessionException"></exception>
        public async Task Login()
        {
            if (!this.BadSession && this.IsLoggedin)
                throw new InvalidOperationException("すでにログインしています");

            if (!this.BadSession && nicosid != null && nicosid.Length > 0 && session != null && session.Length > 0 && secure != null && secure.Length > 0)
            {
                this.cookie = new CookieCollection();

                this.cookie.Add(new Cookie("nicosid", nicosid, "/", "nicovideo.jp"));
                this.cookie.Add(new Cookie("user_session", session, "/", "nicovideo.jp"));
                this.cookie.Add(new Cookie("user_session_secure", secure, "/", "nicovideo.jp"));

                return;
            }

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

            Cookie cookieNicosid = cookieCollection.Where(x => x.Name == "nicosid").Single();
            Cookie cookieSession = cookieCollection.Where(x => x.Name == "user_session").Single();
            Cookie cookieSecure = cookieCollection.Where(x => x.Name == "user_session_secure").Single();

            this.nicosid = cookieNicosid.Value;
            this.session = cookieSession.Value;
            this.secure = cookieSecure.Value;

            this.BadSession = false;

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
            this.nicosid = null;
            this.session = null;
            this.secure = null;
        }
    }
}
