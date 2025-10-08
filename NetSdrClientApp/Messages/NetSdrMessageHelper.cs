namespace NetSdrClientApp.Messages
{
    public static class NetSdrMessageHelper
    {
        public enum MessageType : byte
        {
            Request = 0x00,
            Response = 0x01,
            Control = 0x02,
            Data = 0x03
        }

        public enum RequestCode : byte
        {
            GetDeviceName = 0x01,
            GetDeviceSerialNumber = 0x02,
            GetDeviceInfo = 0x03
        }

        public enum ControlItemCode : byte
        {
            ReceiverState_Off = 0x18,
            ReceiverState_On = 0x80
        }

        public struct Message
        {
            public MessageType Type;
            public byte Code;
            public byte SequenceNumber;
            public byte[]? Data;
        }

        public static byte[] CreateMessage(MessageType type, byte code, byte sequenceNumber, byte[]? data = null)
        {
            int dataLength = data?.Length ?? 0;
            byte[] message = new byte[4 + dataLength];

            message[0] = (byte)type;
            message[1] = code;
            message[2] = sequenceNumber;
            message[3] = (byte)dataLength;

            if (data != null && dataLength > 0)
            {
                Array.Copy(data, 0, message, 4, dataLength);
            }

            return message;
        }

        public static void ParseMessage(byte[] message, out MessageType type, out byte code, out byte sequenceNumber)
        {
            ValidateMessageLength(message);

            type = (MessageType)message[0];
            code = message[1];
            sequenceNumber = message[2];
        }

        private static void ValidateMessageLength(byte[] message)
        {
            if (message == null || message.Length < 4)
            {
                throw new ArgumentException("Message is too short to be valid", nameof(message));
            }
        }

        public static IEnumerable<byte> ExtractDataItems(byte[] message)
        {
            return ExtractDataItemsInternal(message);
        }

        private static IEnumerable<byte> ExtractDataItemsInternal(byte[] message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message), "Message cannot be null");
            }

            if (message.Length < 4)
            {
                throw new ArgumentException("Invalid message format: message too short", nameof(message));
            }

            int dataLength = message[3];

            if (message.Length < 4 + dataLength)
            {
                throw new ArgumentException("Invalid message format: declared data length exceeds message size", nameof(message));
            }

            for (int i = 4; i < 4 + dataLength; i++)
            {
                yield return message[i];
            }
        }
    }
}