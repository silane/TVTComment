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
    [System.Serializable]
    class NiconicoLoginException : Exception
    {
        public NiconicoLoginException() { }
        public NiconicoLoginException(string message) : base(message) { }
        public NiconicoLoginException(string message, Exception inner) : base(message, inner) { }
        protected NiconicoLoginException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    class NiconicoLoginSession
    {
        private string mail;
        private string password;
        private CookieCollection cookie =null;

        public bool IsLoggedin => cookie != null;
        public CookieCollection Cookie
        {
            get
            {
                if (IsLoggedin)
                    return cookie;
                else
                    throw new InvalidOperationException("ログインしていません");
            }
        }

        public NiconicoLoginSession(string mail,string password)
        {
            this.mail = mail;
            this.password = password;
        }

        public async Task Login()
        {
            if (IsLoggedin)
                throw new InvalidOperationException("すでにログインしています");

            const string loginUrl = "https://secure.nicovideo.jp/secure/login?site=niconico";

            using (var handler = new HttpClientHandler())
            {
                using (var client = new HttpClient(handler))
                {
                    var content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "next_url", string.Empty },
                        { "mail", mail },
                        { "password", password }
                    });
                    
                    await client.PostAsync(loginUrl, content).ConfigureAwait(false);

                    CookieCollection cookieCollection = handler.CookieContainer.GetCookies(new Uri(loginUrl));
                    for (int i = 0; i < cookieCollection.Count; i++)
                    {
                        if (cookieCollection[i].Name == "user_session")
                        {
                            cookie = cookieCollection;
                            return;
                        }
                    }
                    throw new NiconicoLoginException("ログインに失敗しました");
                }
            }
        }

        public void Logout()
        {
            if (!IsLoggedin)
                throw new InvalidOperationException("ログインしていません");

            using (var handler = new HttpClientHandler())
            {
                using (var client = new HttpClient(handler))
                {
                    handler.CookieContainer.Add(cookie);
                    client.GetAsync("https://secure.nicovideo.jp/secure/logout").ConfigureAwait(false);
                    cookie = null;
                }
            }
        }
    }
}
