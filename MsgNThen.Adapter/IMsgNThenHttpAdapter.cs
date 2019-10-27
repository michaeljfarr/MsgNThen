using System.Threading.Tasks;
using MsgNThen.Interfaces;

namespace MsgNThen.Adapter
{
    public interface IMsgNThenHttpAdapter
    {
        void Listen();
        void StopListening();
        Task<AckResult> HandleMessageTask(IHandledMessage message);
    }
}