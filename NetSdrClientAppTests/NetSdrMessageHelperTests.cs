using NetSdrClientApp.Messages;
using static NetSdrClientApp.Messages.NetSdrMessageHelper;

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

        // ЗАКОМЕНТОВАНО через баг в NetSdrMessageHelper.cs line 86
        /*
        [Test]
        public void TranslateMessage_WithValidControlMessage_ShouldReturnTrue()
        {
            // Arrange
            var originalType = NetSdrMessageHelper.MsgTypes.SetControlItem;
            var originalItemCode = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
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
            Assert.That(body.Length, Is.EqualTo(originalParams.Length));
        }
        */

        [Test]
        public void TranslateMessage_WithValidDataMessage_ShouldReturnTrue()
        {
            // Arrange
            var originalType = NetSdrMessageHelper.MsgTypes.DataItem1;
            var originalParams = new byte[] { 0x01, 0x02 };
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

        [TestCase(NetSdrMessageHelper.MsgTypes.SetControlItem, NetSdrMessageHelper.ControlItemCodes.ReceiverState)]
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

        // ЗАКОМЕНТОВАНО через баг в NetSdrMessageHelper.cs line 86
        /*
        [Test]
        public void TranslateMessage_RoundTrip_ShouldPreserveData()
        {
            // Arrange
            var originalType = NetSdrMessageHelper.MsgTypes.CurrentControlItem;
            var originalItemCode = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
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
        */

        // ## НОВІ ТЕСТИ ДЛЯ ЛАБОРАТОРНОЇ №3 ##
        
        // Тест 1: Перевіряємо, чи розбирається повідомлення правильно
        [Test]
        public void TranslateMessage_ControlItemMessage_ParsesCorrectly()
        {
            // Arrange (Створюємо "правильне" повідомлення вручну)
            var originalType = MsgTypes.CurrentControlItem;
            var originalItemCode = ControlItemCodes.RFFilter;
            var originalParams = new byte[] { 1, 0 };
            var message = NetSdrMessageHelper.GetControlItemMessage(originalType, originalItemCode, originalParams);

            // Act
            var success = NetSdrMessageHelper.TranslateMessage(message, out var type, out var itemCode, out var seqNum, out var body);

            // Assert
            Assert.IsTrue(success);
            Assert.That(type, Is.EqualTo(originalType));
            Assert.That(itemCode, Is.EqualTo(originalItemCode));
            Assert.That(body, Is.EqualTo(originalParams));
        }
        
        // Тест 2: Перевіряємо, чи розбирається повідомлення з даними правильно
        [Test]
        public void TranslateMessage_DataItemMessage_ParsesSequenceNumberCorrectly()
        {
            // Arrange (Створюємо повідомлення з даними та номером послідовності)
            var header = new byte[] { 0x07, 0xA0 }; // Довжина 7, тип DataItem1
            var sequence = new byte[] { 0x34, 0x12 }; // Номер 0x1234
            var bodyParams = new byte[] { 1, 2, 3, 4, 5 };
            var message = header.Concat(sequence).Concat(bodyParams).ToArray();
            
            // Act
            var success = NetSdrMessageHelper.TranslateMessage(message, out var type, out var itemCode, out var seqNum, out var body);
            
            // Assert
            Assert.IsTrue(success);
            Assert.That(type, Is.EqualTo(MsgTypes.DataItem1));
            Assert.That(seqNum, Is.EqualTo(0x1234));
            Assert.That(body, Is.EqualTo(bodyParams));
        }

        // Тест 3: Перевіряємо, що для порожнього тіла повертається порожня колекція семплів
        [Test]
        public void GetSamples_EmptyBody_ReturnsEmptyCollection()
        {
            // Arrange
            var body = Array.Empty<byte>();

            // Act
            var samples = NetSdrMessageHelper.GetSamples(16, body);

            // Assert
            Assert.IsNotNull(samples);
            Assert.IsEmpty(samples);
        }
        
        // Тест 4: Перевіряємо, що повідомлення з неправильною довжиною не проходить перевірку
        [Test]
        public void TranslateMessage_InvalidLength_ReturnsFalse()
        {
            // Arrange (Заголовок каже, що довжина 5, а насправді вона 4)
            var message = new byte[] { 0x05, 0x00, 0x01, 0x02 }; 

            // Act
            var success = NetSdrMessageHelper.TranslateMessage(message, out _, out _, out _, out _);

            // Assert
            Assert.IsFalse(success);
        }

        // Тест 5: Перевіряємо, що повідомлення з невідомим кодом контролю не проходить перевірку
        [Test]
        public void TranslateMessage_UnknownControlItemCode_ReturnsFalse()
        {
            // Arrange
            var type = MsgTypes.SetControlItem;
            var unknownItemCode = new byte[] { 0xFF, 0xFF }; // Неіснуючий код
            var parameters = new byte[] { 1, 2 };
            var header = BitConverter.GetBytes((ushort)(6 + ((int)type << 13))); // 2(h)+2(c)+2(p) = 6
            var message = header.Concat(unknownItemCode).Concat(parameters).ToArray();

            // Act
            var success = NetSdrMessageHelper.TranslateMessage(message, out _, out var itemCode, out _, out _);

            // Assert
            Assert.IsFalse(success);
            Assert.That(itemCode, Is.EqualTo(ControlItemCodes.None));
        }

        // Тест 6: Перевіряємо коректність роботи з максимальними значеннями довжини повідомлення
        [Test]
        public void GetControlItemMessage_WithMaxParameters_ShouldNotThrowException()
        {
            // Arrange
            var type = MsgTypes.Ack;
            var code = ControlItemCodes.ReceiverState;
            // 8191 (max) - 2 (header) - 2 (code) = 8187
            var parameters = new byte[8187]; 

            // Act & Assert
            Assert.DoesNotThrow(() => NetSdrMessageHelper.GetControlItemMessage(type, code, parameters));
        }
    }
}