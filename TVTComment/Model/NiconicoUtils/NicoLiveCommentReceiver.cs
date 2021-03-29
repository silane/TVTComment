﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TVTComment.Model.NiconicoUtils
{
    class NicoLiveCommentReceiverException : Exception
    {
        public NicoLiveCommentReceiverException() { }
        public NicoLiveCommentReceiverException(string message) : base(message) { }
        public NicoLiveCommentReceiverException(string message, Exception inner) : base(message, inner) { }
    }

    class InvalidPlayerStatusNicoLiveCommentReceiverException : NicoLiveCommentReceiverException
    {
        public string PlayerStatus { get; }
        public InvalidPlayerStatusNicoLiveCommentReceiverException(string playerStatus)
        {
            this.PlayerStatus = playerStatus;
        }
    }

    class NetworkNicoLiveCommentReceiverException : NicoLiveCommentReceiverException
    {
        public NetworkNicoLiveCommentReceiverException(Exception inner) : base(null, inner)
        {
        }
    }
    class ConnectionClosedNicoLiveCommentReceiverException : NicoLiveCommentReceiverException
    {
        public ConnectionClosedNicoLiveCommentReceiverException()
        {
        }
    }

    class NicoLiveCommentReceiver : IDisposable
    {
        public NiconicoLoginSession NiconicoLoginSession { get; }

        public NicoLiveCommentReceiver(NiconicoLoginSession niconicoLoginSession)
        {
            this.NiconicoLoginSession = niconicoLoginSession;

            var handler = new HttpClientHandler();
            handler.CookieContainer.Add(niconicoLoginSession.Cookie);
            this.httpClient = new HttpClient(handler);
            var assembly = Assembly.GetExecutingAssembly().GetName();
            var ua = assembly.Name + "/" + assembly.Version.ToString(3);
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", ua);
        }

        /// <summary>
        /// KeepAliveコマンドの送信
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="NetworkNicoLiveCommentReceiverException"></exception>
        private async void SendBlankAliveMessage(ClientWebSocket ws, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (ws == null || !WebSocketState.Open.Equals(ws.State))
            {
                Debug.WriteLine("websocket client is in wrong state.");
                return;
            }
            while (true)
            {
                try
                {
                    await Task.Delay(60 * 1000, cancellationToken); // 1分待ちます。
                    await ws.SendAsync(Encoding.UTF8.GetBytes(""), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false); //0byteデータ送信
                }
                catch (Exception e) when (e is ObjectDisposedException || e is SocketException || e is IOException || e is TaskCanceledException)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;
                    if (e is TaskCanceledException)
                        return;
                    if (e is ObjectDisposedException)
                        throw;
                    else
                        throw new NetworkNicoLiveCommentReceiverException(e);
                }
            }
        }

        /// <summary>
        /// 受信した<see cref="NiconicoCommentXmlTag"/>を無限非同期イテレータで返す
        /// </summary>
        /// <exception cref="OperationCanceledException"></exception>
        /// <exception cref="InvalidPlayerStatusNicoLiveCommentReceiverException"></exception>
        /// <exception cref="NetworkNicoLiveCommentReceiverException"></exception>
        /// <exception cref="ConnectionClosedNicoLiveCommentReceiverException"></exception>
        public async IAsyncEnumerable<NiconicoCommentXmlTag> Receive(string liveId, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var _ = cancellationToken.Register(() =>
            {
                this.httpClient.CancelPendingRequests();
            });

            for (int disconnectedCount = 0; disconnectedCount < 5; ++disconnectedCount)
            {
                // 万が一接続中断した場合、数秒空いたからリトライする。
                var random = new Random();
                await Task.Delay((disconnectedCount * 5000) + random.Next(0, 101));

                Stream str;
                try
                {
                    str = await this.httpClient.GetStreamAsync($"https://live2.nicovideo.jp/unama/watch/{liveId}/programinfo").ConfigureAwait(false);
                }
                // httpClient.CancelPendingRequestsが呼ばれた、もしくはタイムアウト
                catch (TaskCanceledException e)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(null, e, cancellationToken);
                    throw new NetworkNicoLiveCommentReceiverException(e);
                }
                catch (HttpRequestException e)
                {
                    throw new NetworkNicoLiveCommentReceiverException(e);
                }
                var playerStatus = await JsonDocument.ParseAsync(str, cancellationToken: cancellationToken).ConfigureAwait(false);
                var playerStatusRoot = playerStatus.RootElement;

                if (playerStatusRoot.GetProperty("data").GetProperty("rooms").GetArrayLength() <= 0)
                    throw new InvalidPlayerStatusNicoLiveCommentReceiverException("現在放送されていないか、コミュニティ限定配信のためコメント取得できませんでした");

                var threadId = playerStatusRoot.GetProperty("data").GetProperty("rooms")[0].GetProperty("threadId").GetString();
                var msUriStr = playerStatusRoot.GetProperty("data").GetProperty("rooms")[0].GetProperty("webSocketUri").GetString();
                if (threadId == null || msUriStr == null)
                {
                    throw new InvalidPlayerStatusNicoLiveCommentReceiverException(str.ToString());
                }
                // WebSocketAPIに接続
                ClientWebSocket ws = new ClientWebSocket();
                // UAヘッダ追加
                var assembly = Assembly.GetExecutingAssembly().GetName();
                string version = assembly.Version.ToString(3);
                ws.Options.SetRequestHeader("User-Agent", $"TvtComment/{version}");
                // SubProtocol追加
                ws.Options.AddSubProtocol(WEBSOCKET_PROTOCOL);
                // Sec-WebSocket-Versionヘッダ追加
                ws.Options.SetRequestHeader("Sec-WebSocket-Extensions", WEBSOCKET_EXTENSIONS);

                var uri = new Uri(msUriStr);
                await ws.ConnectAsync(uri, cancellationToken);
                var buffer = new byte[1024];

                // threadId情報を送信
                string body = "[{\"ping\":{\"content\":\"rs:0\"}},{\"ping\":{\"content\":\"ps:0\"}},{\"thread\":{\"thread\":\"" + threadId + "\",\"version\":\"20061206\",\"user_id\":\"guest\",\"res_from\":-10,\"with_global\":1,\"scores\":1,\"nicoru\":0}},{\"ping\":{\"content\":\"pf:0\"}},{\"ping\":{\"content\":\"rf:0\"}}]";
                byte[] bodyEncoded = Encoding.UTF8.GetBytes(body);
                try
                {
                    await ws.SendAsync(bodyEncoded, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (e is ObjectDisposedException || e is SocketException || e is IOException)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(null, e, cancellationToken);
                    if (e is ObjectDisposedException)
                        throw;
                    else
                        throw new NetworkNicoLiveCommentReceiverException(e);
                }

                // 1分間毎に0byteのKeepAliveコマンドを送信。
                SendBlankAliveMessage(ws, cancellationToken);

                //情報取得待ちループ
                while (true)
                {
                    var segment = new ArraySegment<byte>(buffer);
                    var result = await ws.ReceiveAsync(segment, cancellationToken);

                    //エンドポイントCloseの場合、処理を中断
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "OK",
                          cancellationToken);
                        break;
                    }

                    //バイナリの場合は、当処理では扱えないため、処理を中断
                    if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.InvalidMessageType,
                          "Binary not supported.", cancellationToken);
                        break;
                    }

                    int count = result.Count;
                    while (!result.EndOfMessage)
                    {
                        if (count >= buffer.Length)
                        {
                            await ws.CloseAsync(WebSocketCloseStatus.InvalidPayloadData,
                              "That's too long", cancellationToken);
                            throw new ConnectionClosedNicoLiveCommentReceiverException();
                        }
                        segment = new ArraySegment<byte>(buffer, count, buffer.Length - count);
                        result = await ws.ReceiveAsync(segment, cancellationToken);

                        count += result.Count;
                    }

                    //メッセージを取得
                    var message = Encoding.UTF8.GetString(buffer, 0, count);
                    this.parser.Push(message);
                    while (this.parser.DataAvailable())
                        yield return this.parser.Pop();
                }
            }
            throw new ConnectionClosedNicoLiveCommentReceiverException();
        }

        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        private readonly HttpClient httpClient;
        private readonly NiconicoCommentJsonParser parser = new NiconicoCommentJsonParser(true);
        private readonly string WEBSOCKET_PROTOCOL = "msg.nicovideo.jp#json";
        private readonly string WEBSOCKET_EXTENSIONS = "permessage-deflate; client_max_window_bits";
    }
}