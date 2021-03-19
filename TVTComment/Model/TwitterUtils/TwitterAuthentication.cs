using CoreTweet;
using System;
using System.Threading.Tasks;
using static CoreTweet.OAuth;

namespace TVTComment.Model.TwitterUtils
{
    class TwitterException : Exception
    {
        public TwitterException() { }
        public TwitterException(string message) : base(message) { }
        public TwitterException(string message, Exception inner) : base(message, inner) { }
        protected TwitterException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    class TwiiterAuthException : TwitterException
    {
        public TwiiterAuthException(string message) : base(message)
        {
        }
    }

    class TwitterAuthentication
    {
        private Tokens token = null;
        private string annicttoken = null;
        public bool IsLoggedin => token != null;
        private readonly string apikey;
        private readonly string apiseecret;
        private readonly string apiaccesstoken;
        private readonly string apiaccesssecret;
        private readonly OAuthSession authSession;

        public Tokens Token
        {
            get
            {
                return token;
            }
        }
        public string AnnictToken
        {
            get
            {
                return annicttoken;
            }
        }

        public OAuthSession AuthSession
        {
            get
            {
                return authSession;
            }
        }
        public TwitterAuthentication(string apikey, string apiseecret)
        {
            this.apikey = apikey;
            this.apiseecret = apiseecret;
            authSession = Authorize(apikey, apiseecret);
        }

        public TwitterAuthentication(string apikey, string apiseecret, string apiaccesstoken, string apiaccesssecret)
        {
            this.apikey = apikey;
            this.apiseecret = apiseecret;
            this.apiaccesstoken = apiaccesstoken;
            this.apiaccesssecret = apiaccesssecret;
        }

        public void Login()
        {
            if (IsLoggedin)
                throw new InvalidOperationException("すでにログインしています");
            if (string.IsNullOrWhiteSpace(apiaccesssecret) && string.IsNullOrWhiteSpace(apiaccesstoken))
                throw new InvalidOperationException("Access tokenとAccess seecretが空白もしくは問題があります。");
            token = Tokens.Create(apikey, apiseecret, apiaccesstoken, apiaccesssecret);
        }

        public async Task Login(string pin)
        {
            if (IsLoggedin)
                throw new InvalidOperationException("すでにログインしています");
            if (string.IsNullOrWhiteSpace(pin))
                throw new InvalidOperationException("PINが空白もしくは問題があります。");
            token = await OAuth.GetTokensAsync(authSession, pin);
        }

        public void Logout()
        {
            if (!IsLoggedin)
                throw new InvalidOperationException("ログインしていません");
            token = null;
        }
        public void AnnictSet(string token)
        {
            annicttoken = token;
        }
    }
}
