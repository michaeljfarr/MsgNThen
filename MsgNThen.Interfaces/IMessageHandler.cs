using System.Threading.Tasks;

namespace MsgNThen.Interfaces
{
    public interface IMessageHandler
    {
        Task<AckResult> HandleMessageTask(IHandledMessage message);
    }
}