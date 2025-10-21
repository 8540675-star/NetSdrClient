using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EchoServer
{
    public class EchoServer
    {
        private readonly int _port;
        private readonly IMessageHandler _messageHandler;
        private TcpListener _listener;
        private CancellationTokenSource _cancellationTokenSource;

        public EchoServer(int port, IMessageHandler messageHandler)
        {
            _port = port;
            _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            Console.WriteLine($"Server started on port {_port}.");

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    Console.WriteLine("Client connected.");

                    _ = Task.Run(() => HandleClientAsync(client, _cancellationTokenSource.Token));
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }

            Console.WriteLine("Server shutdown.");
        }

        public async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using (NetworkStream stream = client.GetStream())
            {
                try
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;

                    while (!token.IsCancellationRequested && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                    {
                        byte[] receivedData = new byte[bytesRead];
                        Array.Copy(buffer, receivedData, bytesRead);

                        byte[] response = await _messageHandler.HandleMessageAsync(receivedData, token);

                        await stream.WriteAsync(response, 0, response.Length, token);
                        Console.WriteLine($"Echoed {response.Length} bytes to the client.");
                    }
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                finally
                {
                    client.Close();
                    Console.WriteLine("Client disconnected.");
                }
            }
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _listener.Stop();
            _cancellationTokenSource.Dispose();
            Console.WriteLine("Server stopped.");
        }
    }

    public class UdpTimedSender : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly UdpClient _udpClient;
        private Timer _timer;
        private ushort _messageCounter;

        public UdpTimedSender(string host, int port)
        {
            _host = host;
            _port = port;
            _udpClient = new UdpClient();
            _messageCounter = 0;
        }

        public void StartSending(int intervalMilliseconds)
        {
            if (_timer != null)
                throw new InvalidOperationException("Sender is already running.");

            _timer = new Timer(SendMessageCallback, null, 0, intervalMilliseconds);
        }

        public byte[] GenerateMessage()
        {
            Random rnd = new Random();
            byte[] samples = new byte[1024];
            rnd.NextBytes(samples);
            _messageCounter++;

            byte[] msg = new byte[] { 0x04, 0x84 }
                .Concat(BitConverter.GetBytes(_messageCounter))
                .Concat(samples)
                .ToArray();

            return msg;
        }

        private void SendMessageCallback(object state)
        {
            try
            {
                byte[] msg = GenerateMessage();
                var endpoint = new IPEndPoint(IPAddress.Parse(_host), _port);

                _udpClient.Send(msg, msg.Length, endpoint);
                Console.WriteLine($"Message sent to {_host}:{_port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        }

        public void StopSending()
        {
            _timer?.Dispose();
            _timer = null;
        }

        public void Dispose()
        {
            StopSending();
            _udpClient.Dispose();
        }
    }

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var messageHandler = new EchoMessageHandler();
            EchoServer server = new EchoServer(5000, messageHandler);

            _ = Task.Run(() => server.StartAsync());

            string host = "127.0.0.1";
            int port = 60000;
            int intervalMilliseconds = 5000;

            using (var sender = new UdpTimedSender(host, port))
            {
                Console.WriteLine("Press any key to stop sending...");
                sender.StartSending(intervalMilliseconds);

                Console.WriteLine("Press 'q' to quit...");
                while (Console.ReadKey(intercept: true).Key != ConsoleKey.Q)
                {
                }

                sender.StopSending();
                server.Stop();
                Console.WriteLine("Sender stopped.");
            }
        }
    }
}