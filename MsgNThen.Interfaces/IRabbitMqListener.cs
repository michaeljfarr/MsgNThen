namespace MsgNThen.Interfaces
{
    public interface IRabbitMqListener
    {
        int NumTasks { get; }
        bool Listen(string queue, ushort maxThreads);
        void Remove(string queue);
    }
}