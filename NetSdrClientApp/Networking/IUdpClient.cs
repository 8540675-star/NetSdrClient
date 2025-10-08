namespace NetSdrClientApp
{
    public interface IUdpClient
    {
        event EventHandler<byte[]>? MessageReceived;
        void Connect(string host, int port);
        void Send(byte[] data);
        Task<byte[]> ReceiveAsync();
        void Close();
    }
}