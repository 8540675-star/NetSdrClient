using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace NetSdrClientApp.Networking
{
    public class UdpClientWrapper : IUdpClient, IDisposable
    {
        private UdpClient? _client;
        private IPEndPoint? _remoteEndpoint;
        private Task? _receiveTask;
        private CancellationTokenSource? _cts;

        public event EventHandler<byte[]>? MessageReceived;

        public void Connect(string host, int port)
        {
            _client = new UdpClient();
            _remoteEndpoint = new IPEndPoint(IPAddress.Parse(host), port);
            _client.Connect(_remoteEndpoint);

            _cts = new CancellationTokenSource();
            _receiveTask = Task.Run(() => ReceiveLoop(_cts.Token));
        }

        private void ReceiveLoop(CancellationToken cancellationToken)
        {
            if (_client == null) return;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    IPEndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
                    byte[] receivedData = _client.Receive(ref remoteEp);
                    MessageReceived?.Invoke(this, receivedData);
                }
                catch (SocketException)
                {
                    // Socket closed or error
                    break;
                }
            }
        }

        public void Send(byte[] data)
        {
            if (_client == null)
            {
                throw new InvalidOperationException("Not connected");
            }

            _client.Send(data, data.Length);
        }

        public async Task<byte[]> ReceiveAsync()
        {
            if (_client == null)
            {
                throw new InvalidOperationException("Not connected");
            }

            var result = await _client.ReceiveAsync();
            return result.Buffer;
        }

        public void Close()
        {
            _cts?.Cancel();
            _receiveTask?.Wait();
            _client?.Close();
            _client = null;
        }

        public override int GetHashCode()
        {
            if (_client == null || _remoteEndpoint == null)
            {
                return base.GetHashCode();
            }

            // Using SHA256 for demonstration purposes only
            // In production, consider if cryptographic hash is necessary
            using (var sha256 = SHA256.Create())
            {
                var hashInput = $"{_remoteEndpoint.Address}:{_remoteEndpoint.Port}";
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(hashInput));
                return BitConverter.ToInt32(hashBytes, 0);
            }
        }

        public override bool Equals(object? obj)
        {
            if (obj is UdpClientWrapper other)
            {
                return _remoteEndpoint?.Equals(other._remoteEndpoint) ?? false;
            }
            return false;
        }

        public void Dispose()
        {
            _cts?.Dispose();
            _client?.Dispose();
        }
    }
}