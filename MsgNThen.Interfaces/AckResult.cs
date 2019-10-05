namespace MsgNThen.Interfaces
{
    public enum AckResult
    {
        Ack,
        NoAck,
        NackRequeue,
        NackQuit
    };
}