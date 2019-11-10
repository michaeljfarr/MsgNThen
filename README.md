# MsgNThen
Dotnet Utilities based on Redis and RabbitMQ that implement message handlers with dependencies on groups of messages.


* Dotnet core 3.0 Adapter for RabbitMQ
  * Microsoft.AspNetCore.Hosting.Server.IServer implementation
  * Messages recieved by RabbitMQ are handled by standard Dotnet Core Controllers
  * Message handling is optionally tracked by Redis, enabling dependent message delivery
* Uri Broker for RabbitMQ, S3, Redis, Http, SQS
  * This enables the IServer to push "Replies" without the typical HTTP response mechanism.
  
Compare to:
	JasperFx: Jasper replaces the whole dotnet core MVC pipleine, where as this tries to work within it.
	EasyMQ: EasyMQ is an adapter purely for rabbitmq and also replaces the dotnet core MVC pipleine.

Why put so much effort into maintaining the dotnet core pipeline when they dont really do that much?  Most of the useful
applications for this system involve One-Way messaging
 * Many systems are originally built on HTTP and only convert to messaging when scalability issues arise.
   * In many such situations the investment into the messaging is a distraction from business priorities.
   * The business does not want to be locked into the specific message queue technology, and the cost of migrating back to http should also be low.
 * MsgNThen is mostly invisible because it implements IServer, which is typically hidden from dotnet core systems.  However, as expected changing to a messaging based system cannot be achieved without some changes.
   * MsgNThen uses standard API Controllers.  Many controllers will not require code changes, but some configuration may be required to determine how to handle failures (message redelivery or cancellation etc).  
   * MsgNThen uses the standard router, but some changes are require to ensure the queue and routing key correctly match the controller path and that the header "Verb" is povided.
   * MsgNThen relies on the standard authorization filters.  However, since this implementation will only push replies, some additional configuration is required to determine how to deliver the failed responses.
   * Program and Startup is where most changes are required, also is where the configuration of rabbitmq exchange-queue binding should be implemented.  
   * MsgNThen does not interpret http specific features.  Systems like tls client certificates special use of http headers should generally be removed.  However, MgnNThen (and underlying transports) provide similar replacements for these things.
 * Whilst the Controllers do not need to change much, the system that sends the requests would generally need to change - particularly if it was using the HttpClient directly.  


MsgNThen aims to be the simplest transition to queue based technologies as possible for dotnet core based APIs.  The motiviations to switch to queue based transports occur when
 * Fan out is required (multiple requests sent from the same sender)
 * Situations where sequences of interdependent http requests are being orchestrated in proc. (multiple requests sent from the same sender and then transformed into a single result)

 Why queues:
 * Using HTTP RPC is inefficient and complicated as the fan-out ratio gets higher because of the cost of managing large numbers of independent http connections.  RabbitMQ (or any MQ) lets the task distributor fire-and-forget the messages so its tasks/threads can return to handling new requests.
 * Achieving good reliability when orchestrating multiple http connections becomes challenging in a variety of common situations, particularly when system load increases.  Conversely, fire-and-forget isolates concerns of reliability away from the business logic.



