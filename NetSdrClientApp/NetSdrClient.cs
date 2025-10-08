using System.Net;
using System.Text;
using NetSdrClientApp.Networking;
using NetSdrClientApp.Messages;

namespace NetSdrClientApp
{
    public class NetSdrClient
    {
        const int DEFAULT_CONTROL_PORT = 50000;
        const int DEFAULT_DATA_PORT = 50001;

        public IPEndPoint? ServerEndpoint { get; private set; }
        public bool IsConnected { get; private set; }

        private readonly ITcpClient _tcpClient;
        private readonly IUdpClient _udpClient;

        private byte _lastSequenceNumber = 0;
        private readonly object _lockObj = new object();
        private TaskCompletionSource<NetSdrMessageHelper.Message> responseTaskSource = new();

        public NetSdrClient(ITcpClient? tcpClient = null, IUdpClient? udpClient = null)
        {
            _tcpClient = tcpClient ?? new TcpClientWrapper("127.0.0.1", DEFAULT_CONTROL_PORT);
            _udpClient = udpClient ?? new UdpClientWrapper();
            _udpClient.MessageReceived += _udpClient_MessageReceived;
        }

        public async Task ConnectAsync(IPEndPoint? serverEndpoint = null)
        {
            if (serverEndpoint != null)
            {
                ServerEndpoint = serverEndpoint;
            }
            else
            {
                ServerEndpoint = new IPEndPoint(IPAddress.Loopback, DEFAULT_CONTROL_PORT);
            }

            _tcpClient.Connect(ServerEndpoint.Address.ToString(), ServerEndpoint.Port);

            _udpClient.Connect(ServerEndpoint.Address.ToString(), DEFAULT_DATA_PORT);

            IsConnected = true;
            await Task.CompletedTask;
        }

        public async Task DisconnectAsync()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected to server");
            }

            _tcpClient.Close();

            _udpClient.Close();

            IsConnected = false;
            ServerEndpoint = null;
            await Task.CompletedTask;
        }

        private async Task<NetSdrMessageHelper.Message> SendMessageAsync(NetSdrMessageHelper.MessageType type, byte code, byte[]? data = null)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected to server");
            }

            byte sequenceNum;
            lock (_lockObj)
            {
                sequenceNum = _lastSequenceNumber++;
            }

            var request = NetSdrMessageHelper.CreateMessage(type, code, sequenceNum, data);
            _tcpClient.Send(request);

            responseTaskSource = new TaskCompletionSource<NetSdrMessageHelper.Message>();
            var response = await responseTaskSource.Task;

            return response;
        }

        public async Task<string> GetDeviceNameAsync()
        {
            var response = await SendMessageAsync(
                NetSdrMessageHelper.MessageType.Request,
                (byte)NetSdrMessageHelper.RequestCode.GetDeviceName
            );

            if (response.Data != null && response.Data.Length > 0)
            {
                return Encoding.UTF8.GetString(response.Data);
            }

            return string.Empty;
        }

        public async Task<byte[]?> GetDeviceSerialNumberAsync()
        {
            var response = await SendMessageAsync(
                NetSdrMessageHelper.MessageType.Request,
                (byte)NetSdrMessageHelper.RequestCode.GetDeviceSerialNumber
            );

            return response.Data;
        }

        public async Task SetReceiverStateAsync(bool enable)
        {
            byte code = enable
                ? (byte)NetSdrMessageHelper.ControlItemCode.ReceiverState_On
                : (byte)NetSdrMessageHelper.ControlItemCode.ReceiverState_Off;

            await SendMessageAsync(
                NetSdrMessageHelper.MessageType.Control,
                code
            );
        }

        private void _udpClient_MessageReceived(object? sender, byte[] data)
        {
            NetSdrMessageHelper.ParseMessage(data, out var _, out var _, out var _);
            // Response received via UDP, handle if needed
        }

        public async Task<IEnumerable<byte>> ReceiveDataAsync(int count)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected to server");
            }

            List<byte> receivedData = new List<byte>();

            while (receivedData.Count < count)
            {
                var data = await _udpClient.ReceiveAsync();
                receivedData.AddRange(data);
            }

            return receivedData;
        }

        public void ProcessReceivedMessage(byte[] message)
        {
            NetSdrMessageHelper.ParseMessage(message, out var type, out var code, out var sequenceNum);

            if (type == NetSdrMessageHelper.MessageType.Response)
            {
                var parsedMessage = new NetSdrMessageHelper.Message
                {
                    Type = type,
                    Code = code,
                    SequenceNumber = sequenceNum,
                    Data = message.Length > 4 ? message[4..] : null
                };

                responseTaskSource?.TrySetResult(parsedMessage);
            }
        }
    }
}