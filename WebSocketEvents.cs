using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace V275_REST_Lib
{
    public class WebSocketEvents
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private ClientWebSocket? Socket;
        private CancellationTokenSource? SocketLoopTokenSource;

        public delegate void SocketStateDelegate(WebSocketState state, string msg = "");
        public event SocketStateDelegate? SocketState;

        public delegate void MessageRecievedDelegate(string message);
        public event MessageRecievedDelegate? MessageRecieved;

        public delegate void DataRecievedDelegate(byte[] data);
        public event DataRecievedDelegate? DataRecieved;

        public bool IsBinary { get; private set; }

        public async Task<bool> StartAsync(string wsUri, bool isBinary = false)
             => await StartAsync(new Uri(wsUri), isBinary);

        public async Task<bool> StartAsync(Uri wsUri, bool isBinary = false)
        {
            IsBinary = isBinary;

            SocketLoopTokenSource = new CancellationTokenSource();

            Logger.Info("WS Staring: {uri}", wsUri.OriginalString);

            try
            {
                Socket = new ClientWebSocket();
                await Socket.ConnectAsync(wsUri, CancellationToken.None);

                if (Socket.State == WebSocketState.Open)
                {
                    _ = Task.Run(() => SocketProcessingLoopAsync().ConfigureAwait(false));

                    _ = Task.Run(() => SocketState?.Invoke(Socket.State));

                    return true;
                }
                else
                    return false;
            }
            catch (OperationCanceledException)
            {
                Logger.Error("WS ConnectAsync Canceled");
                if (Socket != null)
                    _ = Task.Run(() => SocketState?.Invoke(Socket.State));
                return false;
                // normal upon task/token cancellation, disregard
            }
            catch (Exception e)
            {
                Logger.Error(e, "WS ConnectAsync Exception");
                if (Socket != null)
                    _ = Task.Run(() => SocketState?.Invoke(Socket.State));
                return false;
            }
        }

        public async Task StopAsync()
        {
            Logger.Info("WS Stopping");

            if (Socket == null)
            {
                _ = Task.Run(() => SocketState?.Invoke(WebSocketState.Closed));
                return;
            }

            if (Socket.State != WebSocketState.Open)
            {
                _ = Task.Run(() => SocketState?.Invoke(Socket.State));
                return;
            }
            // close the socket first, because ReceiveAsync leaves an invalid socket (state = aborted) when the token is cancelled
            CancellationTokenSource timeout = new CancellationTokenSource(10000);
            try
            {
                // after this, the socket state will change to CloseSent
                await Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", timeout.Token);
                // now we wait for the server response, which will close the socket
                while (Socket != null && Socket.State != WebSocketState.Closed && !timeout.Token.IsCancellationRequested) ;

                _ = Task.Run(() => SocketState?.Invoke(Socket == null ? WebSocketState.Closed : Socket.State));
            }
            catch (OperationCanceledException)
            {
                _ = Task.Run(() => SocketState?.Invoke(Socket == null ? WebSocketState.Closed : Socket.State));
                // normal upon task/token cancellation, disregard
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "WS Close Output Async Exception");
                _ = Task.Run(() => SocketState?.Invoke(Socket == null ? WebSocketState.Closed : Socket.State));
            }
            // whether we closed the socket or timed out, we cancel the token causing RecieveAsync to abort the socket
            SocketLoopTokenSource?.Cancel();
        }

        private async Task SocketProcessingLoopAsync()
        {
            if (SocketLoopTokenSource == null || Socket == null)
                return;

            CancellationToken cancellationToken = SocketLoopTokenSource.Token;
            string message = "";
            byte[] data = [];

            ArraySegment<byte> buffer = WebSocket.CreateClientBuffer(4096, 4096);
            while (Socket.State != WebSocketState.Closed && !cancellationToken.IsCancellationRequested)
            {
                WebSocketReceiveResult receiveResult = await Socket.ReceiveAsync(buffer, cancellationToken);
                // if the token is cancelled while ReceiveAsync is blocking, the socket state changes to aborted and it can't be used
                if (!cancellationToken.IsCancellationRequested)
                {
                    // the server is notifying us that the connection will close; send acknowledgement
                    if (Socket.State == WebSocketState.CloseReceived && receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        await Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Acknowledge Close frame", CancellationToken.None);
                    }

                    // display text or binary data
                    if (Socket.State == WebSocketState.Open && receiveResult.MessageType != WebSocketMessageType.Close)
                    {
                        if (receiveResult.MessageType == WebSocketMessageType.Text)
                        {
                            message += Encoding.UTF8.GetString(buffer.Array, 0, receiveResult.Count);

                            if (receiveResult.EndOfMessage)
                            {
                                await Task.Run(() => MessageRecieved?.Invoke(message));
                                message = "";
                            }
                        }
                        else
                        {
                            data = data.Concat(buffer.Array).ToArray();

                            if (receiveResult.EndOfMessage)
                            {
                                await Task.Run(() => DataRecieved?.Invoke(data));
                                data = [];
                            }
                        }
                    }


                }

            }

            if (Socket != null)
            {
                Socket.Dispose();
                Socket = null;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
