# MsgNThen
Dotnet Utilities based on Redis and RabbitMQ that implement message handlers with dependencies on groups of messages.


* Dotnet core 3.0 Adapter for RabbitMQ
  * IServer implementation
  * Messages recieved by RabbitMQ are handled by standard Dotnet Core Controllers
  * Message handling is optionally tracked by Redis, enabling dependent message delivery
