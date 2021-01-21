using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TVTComment.Model.NiconicoUtils
{

    [Serializable]
    class NicoLiveCommentSenderException : Exception
    {
        public NicoLiveCommentSenderException() { }
        public NicoLiveCommentSenderException(string message) : base(message) { }
        public NicoLiveCommentSenderException(string message, Exception inner) : base(message, inner) { }
        protected NicoLiveCommentSenderException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    class InvalidPlayerStatusNicoLiveCommentSenderException : NicoLiveCommentSenderException
    {
        public string PlayerStatus { get; }
        public InvalidPlayerStatusNicoLiveCommentSenderException(string playerStatus)
        {
            this.PlayerStatus = playerStatus;
        }
    }

    class NetworkNicoLiveCommentSenderException : NicoLiveCommentSenderException
    {
        public NetworkNicoLiveCommentSenderException(Exception inner) : base(null, inner)
        {
        }
    }
    class ResponseFormatNicoLiveCommentSenderException : NicoLiveCommentSenderException
    {
        public string Response { get; }

        public ResponseFormatNicoLiveCommentSenderException(string response)
        {
            this.Response = response;
        }
    }

    class ResponseErrorNicoLiveCommentSenderException : NicoLiveCommentSenderException
    {
    }

    class NicoLiveCommentSender : IDisposable
    {
        private readonly HttpClient httpClient;

        public NicoLiveCommentSender(NiconicoLoginSession niconicoSession)
        {
            var handler = new HttpClientHandler();
            handler.CookieContainer.Add(niconicoSession.Cookie);
            this.httpClient = new HttpClient(handler);
            var assembly = Assembly.GetExecutingAssembly().GetName();
            var ua = assembly.Name + "/" + assembly.Version.ToString(3);
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", ua);
        }

        /// <summary>
        /// コメントを投稿する
        /// </summary>
        /// <exception cref="InvalidPlayerStatusNicoLiveCommentSenderException"></exception>
        /// <exception cref="NetworkNicoLiveCommentSenderException"></exception>
        /// <exception cref="ResponseFormatNicoLiveCommentSenderException"></exception>
        /// <exception cref="ResponseErrorNicoLiveCommentSenderException"></exception>
        public async Task Send(string liveId, string message, string mail)
        {
            Stream str;
            try
            {
                if (!liveId.StartsWith("lv")) // 代替えAPIではコミュニティ・チャンネルにおけるコメント鯖取得ができないのでlvを取得しに行く
                {
                    var getLiveId = await httpClient.GetStreamAsync($"https://live2.nicovideo.jp/unama/tool/v1/broadcasters/social_group/{liveId}/program").ConfigureAwait(false);
                    var liveIdJson = await JsonDocument.ParseAsync(getLiveId).ConfigureAwait(false);
                    var liveIdRoot = liveIdJson.RootElement;
                    if (!liveIdRoot.GetProperty("meta").GetProperty("errorCode").GetString().Equals("OK")) throw new InvalidPlayerStatusNicoLiveCommentSenderException("コミュニティ・チャンネルが見つかりませんでした");
                    liveId = liveIdRoot.GetProperty("data").GetProperty("nicoliveProgramId").GetString(); // lvから始まるLiveIDに置き換え

                }
                str = await this.httpClient.GetStreamAsync($"https://live2.nicovideo.jp/unama/watch/{liveId}/programinfo").ConfigureAwait(false);
            }
            catch(HttpRequestException e)
            {
                throw new NetworkNicoLiveCommentSenderException(e);
            }

            var playerStatus = await JsonDocument.ParseAsync(str).ConfigureAwait(false);
            var playerStatusRoot = playerStatus.RootElement;

            playerStatusRoot.GetProperty("data").GetProperty("beginAt").TryGetInt64(out long openTime);
            
            if(liveId == null || openTime == 0)
            {
                throw new InvalidPlayerStatusNicoLiveCommentSenderException(playerStatus.ToString());
            }
            // vposは10ミリ秒単位
            long vpos = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 10 - openTime * 100;

            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            };

            HttpResponseMessage response;
            try
            {
                response = await this.httpClient.PostAsync(
                    $"https://api.cas.nicovideo.jp/v1/services/live/programs/{liveId}/comments",
                    new StringContent(JsonSerializer.Serialize(new Dictionary<string, string> {
                        { "message", message },
                        { "command", mail },
                        { "vpos", vpos.ToString() },
                    }, options), Encoding.UTF8, "application/json")
                );
            }
            catch(HttpRequestException e)
            {
                throw new NetworkNicoLiveCommentSenderException(e);
            }
            string responseBody = await response.Content.ReadAsStringAsync();
            JsonDocument responseBodyJson;
            try
            {
                responseBodyJson = JsonDocument.Parse(responseBody);
            }
            catch(JsonException)
            {
                throw new ResponseFormatNicoLiveCommentSenderException(responseBody);
            }

            using(responseBodyJson)
            {
                int status;
                try
                {
                    status = responseBodyJson.RootElement.GetProperty("meta").GetProperty("status").GetInt32();
                }
                catch (Exception e) when (e is InvalidOperationException || e is KeyNotFoundException)
                {
                    throw new ResponseFormatNicoLiveCommentSenderException(responseBody);
                }
                if (status != 200)
                {
                    throw new ResponseErrorNicoLiveCommentSenderException();
                }
            }
        }

        public void Dispose()
        {
            this.httpClient.Dispose();
        }
    }
}
