using NUnit.Framework;
using NetSdrClientApp.Networking;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetSdrClientAppTests.Networking
{
    [TestFixture]
    public class TcpClientWrapperTests
    {
        private const int TestPort = 15555;
        private const string TestHost = "127.0.0.1";
        
#pragma warning disable NUnit1032
        private TcpListener? _testServer;
#pragma warning restore NUnit1032
        private TcpClient? _serverClient;

        [SetUp]
        public void Setup()
        {
            // Start a simple TCP server for testing
            _testServer = new TcpListener(IPAddress.Loopback, TestPort);
            _testServer.Start();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                _serverClient?.Close();
                _serverClient?.Dispose();
                _serverClient = null;
                
                _testServer?.Stop();
                _testServer = null;
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        [Test]
        public async Task Connect_ShouldConnectSuccessfully()
        {
            // Arrange
            using var wrapper = new TcpClientWrapper(TestHost, TestPort);
            var acceptTask = _testServer!.AcceptTcpClientAsync();

            // Act
            wrapper.Connect();
            _serverClient = await acceptTask;

            // Assert
            await Task.Delay(100); // Give time to establish connection
            Assert.That(wrapper.Connected, Is.True);
        }

        [Test]
        public void Connect_WithInvalidHost_ShouldNotThrow()
        {
            // Arrange
            using var wrapper = new TcpClientWrapper("invalid.host.nonexistent", 9999);

            // Act & Assert
            Assert.DoesNotThrow(() => wrapper.Connect());
            Assert.That(wrapper.Connected, Is.False);
        }

        [Test]
        public async Task Connect_WhenAlreadyConnected_ShouldNotReconnect()
        {
            // Arrange
            using var wrapper = new TcpClientWrapper(TestHost, TestPort);
            var acceptTask = _testServer!.AcceptTcpClientAsync();
            wrapper.Connect();
            _serverClient = await acceptTask;
            await Task.Delay(100);

            // Act
            wrapper.Connect(); // Try to connect again

            // Assert
            Assert.That(wrapper.Connected, Is.True);
        }

        [Test]
        public async Task SendMessageAsync_ShouldSendData()
        {
            // Arrange
            using var wrapper = new TcpClientWrapper(TestHost, TestPort);
            var acceptTask = _testServer!.AcceptTcpClientAsync();
            wrapper.Connect();
            _serverClient = await acceptTask;
            await Task.Delay(100);

            byte[] testData = Encoding.UTF8.GetBytes("Hello");

            // Act
            await wrapper.SendMessageAsync(testData);

            // Assert - read on server side
            byte[] buffer = new byte[1024];
            var stream = _serverClient.GetStream();
            var memoryBuffer = new Memory<byte>(buffer);
            var bytesRead = await stream.ReadAsync(memoryBuffer, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(bytesRead, Is.GreaterThan(0));
                Assert.That(Encoding.UTF8.GetString(buffer, 0, bytesRead), Is.EqualTo("Hello"));
            }
        }

        [Test]
        public async Task SendMessageAsync_String_ShouldSendData()
        {
            // Arrange
            using var wrapper = new TcpClientWrapper(TestHost, TestPort);
            var acceptTask = _testServer!.AcceptTcpClientAsync();
            wrapper.Connect();
            _serverClient = await acceptTask;
            await Task.Delay(100);

            // Act
            await wrapper.SendMessageAsync("Test Message");

            // Assert
            byte[] buffer = new byte[1024];
            var stream = _serverClient.GetStream();
            var memoryBuffer = new Memory<byte>(buffer);
            var bytesRead = await stream.ReadAsync(memoryBuffer, CancellationToken.None);

            Assert.That(Encoding.UTF8.GetString(buffer, 0, bytesRead), Is.EqualTo("Test Message"));
        }

        [Test]
        public void SendMessageAsync_WhenNotConnected_ShouldThrow()
        {
            // Arrange
            using var wrapper = new TcpClientWrapper(TestHost, TestPort);
            byte[] testData = Encoding.UTF8.GetBytes("Hello");

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await wrapper.SendMessageAsync(testData));
        }

        [Test]
        public async Task MessageReceived_ShouldFireWhenDataArrives()
        {
            // Arrange
            using var wrapper = new TcpClientWrapper(TestHost, TestPort);
            var acceptTask = _testServer!.AcceptTcpClientAsync();
            wrapper.Connect();
            _serverClient = await acceptTask;
            await Task.Delay(100);

            byte[]? receivedData = null;
            var messageReceivedEvent = new TaskCompletionSource<bool>();

            wrapper.MessageReceived += (sender, data) =>
            {
                receivedData = data;
                messageReceivedEvent.SetResult(true);
            };

            // Act - send data from server to client
            byte[] testData = Encoding.UTF8.GetBytes("Server Response");
            await _serverClient.GetStream().WriteAsync(new ReadOnlyMemory<byte>(testData), CancellationToken.None);

            // Wait for event with timeout
            var completedTask = await Task.WhenAny(messageReceivedEvent.Task, Task.Delay(3000));

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(completedTask, Is.EqualTo(messageReceivedEvent.Task), "MessageReceived event should fire");
                Assert.That(receivedData, Is.Not.Null);
                Assert.That(Encoding.UTF8.GetString(receivedData!), Is.EqualTo("Server Response"));
            }
        }

        [Test]
        public async Task Disconnect_ShouldCloseConnection()
        {
            // Arrange
            using var wrapper = new TcpClientWrapper(TestHost, TestPort);
            var acceptTask = _testServer!.AcceptTcpClientAsync();
            wrapper.Connect();
            _serverClient = await acceptTask;
            await Task.Delay(100);

            // Act
            wrapper.Disconnect();
            await Task.Delay(100);

            // Assert
            Assert.That(wrapper.Connected, Is.False);
        }

        [Test]
        public void Disconnect_WhenNotConnected_ShouldNotThrow()
        {
            // Arrange
            using var wrapper = new TcpClientWrapper(TestHost, TestPort);

            // Act & Assert
            Assert.DoesNotThrow(() => wrapper.Disconnect());
        }

        [Test]
        public async Task Connected_ShouldReturnCorrectState()
        {
            // Arrange
            using var wrapper = new TcpClientWrapper(TestHost, TestPort);

            // Assert - not connected
            Assert.That(wrapper.Connected, Is.False);

            // Connect
            var acceptTask = _testServer!.AcceptTcpClientAsync();
            wrapper.Connect();
            _serverClient = await acceptTask;
            await Task.Delay(100);

            Assert.That(wrapper.Connected, Is.True);

            // Disconnect
            wrapper.Disconnect();
            await Task.Delay(100);

            Assert.That(wrapper.Connected, Is.False);
        }

        [Test]
        public async Task Dispose_ShouldCloseConnection()
        {
            // Arrange
            var wrapper = new TcpClientWrapper(TestHost, TestPort);
            var acceptTask = _testServer!.AcceptTcpClientAsync();
            wrapper.Connect();
            _serverClient = await acceptTask;
            await Task.Delay(100);

            // Act
            wrapper.Dispose();
            await Task.Delay(100);

            // Assert
            Assert.That(wrapper.Connected, Is.False);
        }

        [Test]
        public void Dispose_MultipleTimes_ShouldNotThrow()
        {
            // Arrange
            var wrapper = new TcpClientWrapper(TestHost, TestPort);

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                wrapper.Dispose();
                wrapper.Dispose();
                wrapper.Dispose();
            });
        }

        [Test]
        public async Task SendMessageAsync_WithEmptyData_ShouldNotThrow()
        {
            // Arrange
            using var wrapper = new TcpClientWrapper(TestHost, TestPort);
            var acceptTask = _testServer!.AcceptTcpClientAsync();
            wrapper.Connect();
            _serverClient = await acceptTask;
            await Task.Delay(100);

            byte[] emptyData = Array.Empty<byte>();

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
                await wrapper.SendMessageAsync(emptyData));
        }

        [Test]
        public async Task SendMessageAsync_WithLargeData_ShouldWork()
        {
            // Arrange
            using var wrapper = new TcpClientWrapper(TestHost, TestPort);
            var acceptTask = _testServer!.AcceptTcpClientAsync();
            wrapper.Connect();
            _serverClient = await acceptTask;
            await Task.Delay(100);

            byte[] largeData = new byte[8192];
            for (int i = 0; i < largeData.Length; i++)
            {
                largeData[i] = (byte)(i % 256);
            }

            // Act
            await wrapper.SendMessageAsync(largeData);

            // Assert
            byte[] buffer = new byte[10000];
            var stream = _serverClient.GetStream();
            var memoryBuffer = new Memory<byte>(buffer);
            var bytesRead = await stream.ReadAsync(memoryBuffer, CancellationToken.None);

            Assert.That(bytesRead, Is.EqualTo(8192));
        }
    }
}
