using System.Threading;
using System.Threading.Tasks;

namespace EchoServer
{
    public class EchoMessageHandler : IMessageHandler
    {
        public Task<byte[]> HandleMessageAsync(byte[] data, CancellationToken token)
        {
            // Simply echo back the data
            return Task.FromResult(data);
        }
    }
}