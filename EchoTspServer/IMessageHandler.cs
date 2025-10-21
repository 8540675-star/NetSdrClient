using System.Threading;
using System.Threading.Tasks;

namespace EchoServer
{
    public interface IMessageHandler
    {
        Task<byte[]> HandleMessageAsync(byte[] data, CancellationToken token);
    }
}