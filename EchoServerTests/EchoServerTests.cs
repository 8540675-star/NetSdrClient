using NUnit.Framework;
using EchoServer;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace EchoServerTests
{
    [TestFixture]
    public class EchoMessageHandlerTests
    {
        private EchoMessageHandler _handler;

        [SetUp]
        public void Setup()
        {
            _handler = new EchoMessageHandler();
        }

        [Test]
        public async Task HandleMessageAsync_ReturnsTheSameData()
        {
            // Arrange
            byte[] inputData = Encoding.UTF8.GetBytes("Hello, World!");
            var token = CancellationToken.None;

            // Act
            byte[] result = await _handler.HandleMessageAsync(inputData, token);

            // Assert
            Assert.That(result, Is.EqualTo(inputData));
        }

        [Test]
        public async Task HandleMessageAsync_WithEmptyData_ReturnsEmptyData()
        {
            // Arrange
            byte[] inputData = Array.Empty<byte>();
            var token = CancellationToken.None;

            // Act
            byte[] result = await _handler.HandleMessageAsync(inputData, token);

            // Assert
            Assert.That(result, Has.Length.EqualTo(0));
        }

        [Test]
        public async Task HandleMessageAsync_WithBinaryData_ReturnsTheSameBinaryData()
        {
            // Arrange
            byte[] inputData = new byte[] { 0x01, 0x02, 0x03, 0xFF, 0xAB };
            var token = CancellationToken.None;

            // Act
            byte[] result = await _handler.HandleMessageAsync(inputData, token);

            // Assert
            Assert.That(result, Is.EqualTo(inputData));
        }
    }

    [TestFixture]
    public class UdpTimedSenderTests
    {
        [Test]
        public void Constructor_InitializesCorrectly()
        {
            // Arrange & Act
            using (var sender = new UdpTimedSender("127.0.0.1", 5000))
            {
                // Assert
                Assert.That(sender, Is.Not.Null);
            }
        }

        [Test]
        public void GenerateMessage_CreatesValidMessage()
        {
            // Arrange
            using (var sender = new UdpTimedSender("127.0.0.1", 5000))
            {
                // Act
                byte[] message = sender.GenerateMessage();

                // Assert
                Assert.Multiple(() =>
                {
                    Assert.That(message, Is.Not.Null);
                    Assert.That(message, Has.Length.GreaterThan(2));
                    Assert.That(message[0], Is.EqualTo(0x04));
                    Assert.That(message[1], Is.EqualTo(0x84));
                });
            }
        }

        [Test]
        public void GenerateMessage_IncrementsCounter()
        {
            // Arrange
            using (var sender = new UdpTimedSender("127.0.0.1", 5000))
            {
                // Act
                byte[] message1 = sender.GenerateMessage();
                byte[] message2 = sender.GenerateMessage();

                // Extract counter from messages (bytes 2-3 for ushort)
                ushort counter1 = BitConverter.ToUInt16(message1, 2);
                ushort counter2 = BitConverter.ToUInt16(message2, 2);

                // Assert
                Assert.Multiple(() =>
                {
                    Assert.That(counter1, Is.EqualTo(1));
                    Assert.That(counter2, Is.EqualTo(2));
                });
            }
        }

        [Test]
        public void StartSending_ThrowsIfAlreadyRunning()
        {
            // Arrange
            using (var sender = new UdpTimedSender("127.0.0.1", 5000))
            {
                sender.StartSending(1000);

                // Act & Assert
                Assert.Throws<InvalidOperationException>(() => sender.StartSending(1000));

                sender.StopSending();
            }
        }
    }

    [TestFixture]
    public class EchoServerTests
    {
        [Test]
        public void Constructor_WithNullHandler_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new EchoServer.EchoServer(5000, null!));
        }

        [Test]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange
            var handler = new EchoMessageHandler();

            // Act
            var server = new EchoServer.EchoServer(5000, handler);

            // Assert
            Assert.That(server, Is.Not.Null);
        }
    }
}
