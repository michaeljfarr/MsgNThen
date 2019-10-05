namespace MsgNThen.Interfaces
{
    public interface IRabbitMqListener
    {
        int NumTasks { get; }
        void Listen(string queue, ushort maxThreads);
    }
}