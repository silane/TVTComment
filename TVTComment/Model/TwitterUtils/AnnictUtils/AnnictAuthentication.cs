using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

namespace TVTComment.Model.TwitterUtils.AnnictUtils
{
    class AnnictAuthentication
    {
        private readonly string HOST = $"https://annict.jp/";
        private readonly string ResponseType = "code";
        private readonly string RedirectUri = "urn:ietf:wg:oauth:2.0:oob";
        private readonly string Scope = "read";
        private readonly string ClientId;
        private readonly string ClientSecret;


        public AnnictAuthentication(string clientId, string clientSecret)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
        }

        public Uri GetAuthorizeUri()
        {
            const string endpoint = "oauth/authorize";
            return new(@$"{HOST}{endpoint}?client_id={ClientId}&response_type={ResponseType}&redirect_uri={RedirectUri}&scope={Scope}");
        }

        public string GetToken(string token)
        {
            const string endpoint = "oauth/token";
            using var web = new WebClient();
            var postBodyParameters = new NameValueCollection();
            postBodyParameters.Add("client_id", ClientId);
            postBodyParameters.Add("client_secret", ClientSecret);
            postBodyParameters.Add("grant_type", "authorization_code");
            postBodyParameters.Add("redirect_uri", RedirectUri);
            postBodyParameters.Add("code", token);
            try
            {
                var vs = web.UploadValues(@$"{HOST}{endpoint}", postBodyParameters);
                var json = JsonDocument.Parse(Encoding.UTF8.GetString(vs)).RootElement;
                return json.GetProperty("access_token").GetString();
            }
            catch (WebException e)
            {
                var message = e.Message;
                var stream = e.Response.GetResponseStream();
                if (e.Response != null && stream != null)
                {
                    using var sr = new StreamReader(stream);
                    message += $"\n{sr.ReadToEnd()}";
                    sr.Dispose();
                }
                throw new AnnictException($"Annict API /oauth/token の処理に失敗しました\n\n{message}");
            }
            catch  (Exception e) when (e is KeyNotFoundException || e is JsonException)
            {
                throw new AnnictResponseException("Annict APIからのレスポンスを正しく処理できませんでした");
            }
        }
    }
}
