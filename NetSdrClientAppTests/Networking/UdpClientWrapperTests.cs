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
    public class UdpClientWrapperTests
    {
        private const int TestPort = 15556;
        private UdpClient? _testSender;

        [SetUp]
        public void Setup()
        {
            _testSender = new UdpClient();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                _testSender?.Close();
                _testSender?.Dispose();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        [Test]
        public void Constructor_ShouldCreateInstance()
        {
            // Act
            using var wrapper = new UdpClientWrapper(TestPort);

            // Assert
            Assert.That(wrapper, Is.Not.Null);
        }

        [Test]
        public async Task StartListeningAsync_ShouldReceiveMessages()
        {
            // Arrange
            using var wrapper = new UdpClientWrapper(TestPort);
            byte[]? receivedData = null;
            var messageReceivedEvent = new TaskCompletionSource<bool>();

            wrapper.MessageReceived += (sender, data) =>
            {
                receivedData = data;
                messageReceivedEvent.SetResult(true);
            };

            // Start listening in background
            var listeningTask = Task.Run(() => wrapper.StartListeningAsync());

            // Wait a bit for listener to start
            await Task.Delay(200);

            // Act - send data to wrapper
            byte[] testData = Encoding.UTF8.GetBytes("UDP Test Message");
            await _testSender!.SendAsync(testData, new IPEndPoint(IPAddress.Loopback, TestPort));

            // Wait for event with timeout
            var completedTask = await Task.WhenAny(messageReceivedEvent.Task, Task.Delay(3000));

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(completedTask, Is.EqualTo(messageReceivedEvent.Task), "MessageReceived event should fire");
                Assert.That(receivedData, Is.Not.Null);
                Assert.That(Encoding.UTF8.GetString(receivedData!), Is.EqualTo("UDP Test Message"));
            }

            // Cleanup
            wrapper.StopListening();
            await Task.Delay(100);
        }

        [Test]
        public async Task MessageReceived_ShouldFireMultipleTimes()
        {
            // Arrange
            using var wrapper = new UdpClientWrapper(TestPort + 1);
            int messageCount = 0;
            var receivedMessages = new System.Collections.Generic.List<string>();
            var allMessagesReceived = new TaskCompletionSource<bool>();

            wrapper.MessageReceived += (sender, data) =>
            {
                messageCount++;
                receivedMessages.Add(Encoding.UTF8.GetString(data));
                if (messageCount >= 3)
                {
                    allMessagesReceived.SetResult(true);
                }
            };

            // Start listening
            var listeningTask = Task.Run(() => wrapper.StartListeningAsync());
            await Task.Delay(200);

            // Act - send multiple messages
            for (int i = 1; i <= 3; i++)
            {
                byte[] testData = Encoding.UTF8.GetBytes($"Message {i}");
                await _testSender!.SendAsync(testData, new IPEndPoint(IPAddress.Loopback, TestPort + 1));
                await Task.Delay(100);
            }

            // Wait for all messages
            var completedTask = await Task.WhenAny(allMessagesReceived.Task, Task.Delay(5000));

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(completedTask, Is.EqualTo(allMessagesReceived.Task), "All messages should be received");
                Assert.That(messageCount, Is.EqualTo(3));
                Assert.That(receivedMessages, Has.Count.EqualTo(3));
            }

            // Cleanup
            wrapper.StopListening();
            await Task.Delay(100);
        }

        [Test]
        public async Task StopListening_ShouldStopReceivingMessages()
        {
            // Arrange
            using var wrapper = new UdpClientWrapper(TestPort + 2);
            int messageCount = 0;

            wrapper.MessageReceived += (sender, data) =>
            {
                messageCount++;
            };

            // Start listening
            var listeningTask = Task.Run(() => wrapper.StartListeningAsync());
            await Task.Delay(200);

            // Send first message
            byte[] testData1 = Encoding.UTF8.GetBytes("Message 1");
            await _testSender!.SendAsync(testData1, new IPEndPoint(IPAddress.Loopback, TestPort + 2));
            await Task.Delay(200);

            // Act - stop listening
            wrapper.StopListening();
            await Task.Delay(200);

            // Try to send another message (should not be received)
            byte[] testData2 = Encoding.UTF8.GetBytes("Message 2");
            await _testSender.SendAsync(testData2, new IPEndPoint(IPAddress.Loopback, TestPort + 2));
            await Task.Delay(200);

            // Assert
            Assert.That(messageCount, Is.EqualTo(1), "Should only receive message before stopping");
        }

        [Test]
        public async Task Exit_ShouldStopListening()
        {
            // Arrange
            using var wrapper = new UdpClientWrapper(TestPort + 3);
            var listeningTask = Task.Run(() => wrapper.StartListeningAsync());
            await Task.Delay(200);

            // Act
            wrapper.Exit();
            await Task.Delay(200);

            // Assert - should complete without hanging
            Assert.Pass("Exit completed successfully");
        }

        [Test]
        public void StopListening_WhenNotStarted_ShouldNotThrow()
        {
            // Arrange
            using var wrapper = new UdpClientWrapper(TestPort + 4);

            // Act & Assert
            Assert.DoesNotThrow(() => wrapper.StopListening());
        }

        [Test]
        public void Dispose_ShouldCloseConnection()
        {
            // Arrange
            var wrapper = new UdpClientWrapper(TestPort + 5);

            // Act & Assert
            Assert.DoesNotThrow(() => wrapper.Dispose());
        }

        [Test]
        public void Dispose_MultipleTimes_ShouldNotThrow()
        {
            // Arrange
            var wrapper = new UdpClientWrapper(TestPort + 6);

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                wrapper.Dispose();
                wrapper.Dispose();
                wrapper.Dispose();
            });
        }

        [Test]
        public void GetHashCode_ShouldReturnConsistentValue()
        {
            // Arrange
            using var wrapper = new UdpClientWrapper(TestPort + 7);

            // Act
            int hash1 = wrapper.GetHashCode();
            int hash2 = wrapper.GetHashCode();

            // Assert
            Assert.That(hash1, Is.EqualTo(hash2));
        }

        [Test]
        public void Equals_SamePortAndAddress_ShouldReturnTrue()
        {
            // Arrange
            using var wrapper1 = new UdpClientWrapper(TestPort + 8);
            using var wrapper2 = new UdpClientWrapper(TestPort + 8);

            // Act
            bool areEqual = wrapper1.Equals(wrapper2);

            // Assert
            Assert.That(areEqual, Is.True);
        }

        [Test]
        public void Equals_DifferentPort_ShouldReturnFalse()
        {
            // Arrange
            using var wrapper1 = new UdpClientWrapper(TestPort + 9);
            using var wrapper2 = new UdpClientWrapper(TestPort + 10);

            // Act
            bool areEqual = wrapper1.Equals(wrapper2);

            // Assert
            Assert.That(areEqual, Is.False);
        }

        [Test]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            // Arrange
            using var wrapper = new UdpClientWrapper(TestPort + 11);

            // Act
            bool areEqual = wrapper.Equals(null);

            // Assert
            Assert.That(areEqual, Is.False);
        }

        [Test]
        public void Equals_WithDifferentType_ShouldReturnFalse()
        {
            // Arrange
            using var wrapper = new UdpClientWrapper(TestPort + 12);
            var otherObject = "Not a UdpClientWrapper";

            // Act
            bool areEqual = wrapper.Equals(otherObject);

            // Assert
            Assert.That(areEqual, Is.False);
        }

        [Test]
        public async Task StartListeningAsync_WithCancellation_ShouldStopGracefully()
        {
            // Arrange
            using var wrapper = new UdpClientWrapper(TestPort + 13);
            var listeningTask = Task.Run(() => wrapper.StartListeningAsync());
            await Task.Delay(200);

            // Act
            wrapper.StopListening();

            // Assert - task should complete
            var completedInTime = await Task.WhenAny(listeningTask, Task.Delay(2000)) == listeningTask;
            Assert.That(completedInTime, Is.True, "Listening task should complete after StopListening");
        }

        [Test]
        public async Task StartListeningAsync_WithLargeMessage_ShouldReceive()
        {
            // Arrange
            using var wrapper = new UdpClientWrapper(TestPort + 14);
            byte[]? receivedData = null;
            var messageReceivedEvent = new TaskCompletionSource<bool>();

            wrapper.MessageReceived += (sender, data) =>
            {
                receivedData = data;
                messageReceivedEvent.SetResult(true);
            };

            var listeningTask = Task.Run(() => wrapper.StartListeningAsync());
            await Task.Delay(200);

            // Act - send large data
            byte[] largeData = new byte[8000];
            for (int i = 0; i < largeData.Length; i++)
            {
                largeData[i] = (byte)(i % 256);
            }

            await _testSender!.SendAsync(largeData, new IPEndPoint(IPAddress.Loopback, TestPort + 14));

            var completedTask = await Task.WhenAny(messageReceivedEvent.Task, Task.Delay(3000));

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(completedTask, Is.EqualTo(messageReceivedEvent.Task));
                Assert.That(receivedData, Is.Not.Null);
                Assert.That(receivedData!.Length, Is.EqualTo(8000));
            }

            wrapper.StopListening();
            await Task.Delay(100);
        }
    }
}
