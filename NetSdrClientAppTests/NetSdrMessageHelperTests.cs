using NetSdrClientApp.Messages;

namespace NetSdrClientAppTests
{
    public class NetSdrMessageHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void GetControlItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            int parametersLength = 7500;
            //Act
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);
            var headerBytes = msg.Take(2);
            var codeBytes = msg.Skip(2).Take(2);
            var parametersBytes = msg.Skip(4);
            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);
            var actualCode = BitConverter.ToInt16(codeBytes.ToArray());
            //Assert
            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));
            Assert.That(actualCode, Is.EqualTo((short)code));
            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void GetDataItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem2;
            int parametersLength = 7500;
            //Act
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);
            var headerBytes = msg.Take(2);
            var parametersBytes = msg.Skip(2);
            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);
            //Assert
            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));
            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void TranslateMessage_WithValidControlMessage_ShouldReturnTrue()
        {
            // Arrange
            var originalType = NetSdrMessageHelper.MsgTypes.SetControlItem;
            var originalItemCode = NetSdrMessageHelper.ControlItemCodes.ReceiverFrequency;
            var originalParams = new byte[] { 0x10, 0x20 };
            var message = NetSdrMessageHelper.GetControlItemMessage(originalType, originalItemCode, originalParams);

            // Act
            var success = NetSdrMessageHelper.TranslateMessage(
                message,
                out var type,
                out var itemCode,
                out var sequenceNumber,
                out var body
            );

            // Assert
            Assert.That(success, Is.True);
            Assert.That(type, Is.EqualTo(originalType));
            Assert.That(itemCode, Is.EqualTo(originalItemCode));
        }

        [Test]
        public void TranslateMessage_WithValidDataMessage_ShouldReturnTrue()
        {
            // Arrange
            var originalType = NetSdrMessageHelper.MsgTypes.DataItem1;
            var originalParams = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var message = NetSdrMessageHelper.GetDataItemMessage(originalType, originalParams);

            // Act
            var success = NetSdrMessageHelper.TranslateMessage(
                message,
                out var type,
                out var itemCode,
                out var sequenceNumber,
                out var body
            );

            // Assert
            Assert.That(success, Is.True);
            Assert.That(type, Is.EqualTo(originalType));
            Assert.That(body.Length, Is.EqualTo(originalParams.Length));
        }

        [Test]
        public void GetSamples_With16BitSamples_ShouldReturnCorrectCount()
        {
            // Arrange
            ushort sampleSize = 16; // 2 bytes
            var body = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };

            // Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

            // Assert
            Assert.That(samples.Count, Is.EqualTo(3)); // 6 bytes / 2 = 3 samples
        }

        [Test]
        public void GetSamples_With8BitSamples_ShouldReturnCorrectCount()
        {
            // Arrange
            ushort sampleSize = 8; // 1 byte
            var body = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

            // Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

            // Assert
            Assert.That(samples.Count, Is.EqualTo(5));
        }

        [Test]
        public void GetSamples_With32BitSamples_ShouldReturnCorrectCount()
        {
            // Arrange
            ushort sampleSize = 32; // 4 bytes
            var body = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };

            // Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

            // Assert
            Assert.That(samples.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetSamples_WithInvalidSampleSize_ShouldThrowException()
        {
            // Arrange
            ushort sampleSize = 64; // більше 32 біт
            var body = new byte[] { 0x01, 0x02 };

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                NetSdrMessageHelper.GetSamples(sampleSize, body).ToList()
            );
        }

        [TestCase(NetSdrMessageHelper.MsgTypes.SetControlItem, NetSdrMessageHelper.ControlItemCodes.ReceiverFrequency)]
        [TestCase(NetSdrMessageHelper.MsgTypes.CurrentControlItem, NetSdrMessageHelper.ControlItemCodes.RFFilter)]
        [TestCase(NetSdrMessageHelper.MsgTypes.ControlItemRange, NetSdrMessageHelper.ControlItemCodes.ADModes)]
        [TestCase(NetSdrMessageHelper.MsgTypes.Ack, NetSdrMessageHelper.ControlItemCodes.ReceiverState)]
        public void GetControlItemMessage_WithDifferentTypes_ShouldWork(
            NetSdrMessageHelper.MsgTypes type,
            NetSdrMessageHelper.ControlItemCodes itemCode)
        {
            // Arrange
            var parameters = new byte[] { 0xFF, 0xAA };

            // Act
            var result = NetSdrMessageHelper.GetControlItemMessage(type, itemCode, parameters);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.GreaterThanOrEqualTo(4)); // header + itemCode
        }

        [TestCase(NetSdrMessageHelper.MsgTypes.DataItem0)]
        [TestCase(NetSdrMessageHelper.MsgTypes.DataItem1)]
        [TestCase(NetSdrMessageHelper.MsgTypes.DataItem2)]
        [TestCase(NetSdrMessageHelper.MsgTypes.DataItem3)]
        public void GetDataItemMessage_WithDifferentDataTypes_ShouldWork(NetSdrMessageHelper.MsgTypes type)
        {
            // Arrange
            var parameters = new byte[] { 0x01, 0x02, 0x03 };

            // Act
            var result = NetSdrMessageHelper.GetDataItemMessage(type, parameters);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.GreaterThan(0));
        }

        [Test]
        public void GetControlItemMessage_WithEmptyParameters_ShouldWork()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.SetControlItem;
            var itemCode = NetSdrMessageHelper.ControlItemCodes.IQOutputDataSampleRate;
            var parameters = Array.Empty<byte>();

            // Act
            var result = NetSdrMessageHelper.GetControlItemMessage(type, itemCode, parameters);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(4)); // header + itemCode only
        }

        [Test]
        public void TranslateMessage_RoundTrip_ShouldPreserveData()
        {
            // Arrange
            var originalType = NetSdrMessageHelper.MsgTypes.CurrentControlItem;
            var originalItemCode = NetSdrMessageHelper.ControlItemCodes.ADModes;
            var originalParams = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
            var message = NetSdrMessageHelper.GetControlItemMessage(originalType, originalItemCode, originalParams);

            // Act
            var success = NetSdrMessageHelper.TranslateMessage(
                message,
                out var type,
                out var itemCode,
                out var sequenceNumber,
                out var body
            );

            // Assert
            Assert.That(success, Is.True);
            Assert.That(type, Is.EqualTo(originalType));
            Assert.That(itemCode, Is.EqualTo(originalItemCode));
            Assert.That(body, Is.EqualTo(originalParams));
        }
    }
}