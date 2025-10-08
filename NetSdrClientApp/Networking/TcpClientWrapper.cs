using System.Net.Sockets;

namespace NetSdrClientApp
{
    public class TcpClientWrapper : ITcpClient, IDisposable
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly string _host;
        private readonly int _port;

        private Task? _receiveTask;
        private CancellationTokenSource _cts = new();

        public event EventHandler<byte[]>? MessageReceived;

        public TcpClientWrapper(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public void Connect(string host, int port)
        {
            _client = new TcpClient();
            _client.Connect(host, port);
            _stream = _client.GetStream();

            _cts = new CancellationTokenSource();
            _receiveTask = Task.Run(() => ReceiveLoop(_cts.Token));
        }

        public void Send(byte[] data)
        {
            if (_stream == null)
            {
                throw new InvalidOperationException("Not connected");
            }

            _stream.Write(data, 0, data.Length);
        }

        public void Close()
        {
            _cts?.Cancel();
            _receiveTask?.Wait();
            _stream?.Close();
            _client?.Close();
            _stream = null;
            _client = null;
        }

        private void ReceiveLoop(CancellationToken cancellationToken)
        {
            if (_stream == null) return;

            byte[] buffer = new byte[4096];

            try
            {
                while (!cancellationToken.IsCancellationRequested && _stream.CanRead)
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        byte[] message = new byte[bytesRead];
                        Array.Copy(buffer, message, bytesRead);
                        MessageReceived?.Invoke(this, message);
                    }
                }
            }
            catch (Exception)
            {
                // Connection closed or error occurred
            }
        }

        public void Dispose()
        {
            _cts?.Dispose();
            _stream?.Dispose();
            _client?.Dispose();
        }
    }
}