namespace NetSdrClientApp.Networking
{
    public interface ITcpClient
    {
        event EventHandler<byte[]>? MessageReceived;
        void Connect(string host, int port);
        void Send(byte[] data);
        void Close();
    }
}