namespace MsgNThen.Rabbit
{
    public class RabbitConfig
    {
        public string UserName { get; set; } = "guest";

        /// <summary>Virtual host to access during this connection.</summary>
        public string VirtualHost { get; set; } = "/";
        public string Password { get; set; } = "guest";
        public int Port { get; set; } = -1;
        public string[] HostNames { get; set; } = new []{"localhost"};
    }
}
