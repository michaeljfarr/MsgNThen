# MsgNThen
Dotnet Utilities based on Redis and RabbitMQ that implement message handlers with dependencies on groups of messages.


* Dotnet core 3.0 Adapter for RabbitMQ
  * IServer implementation
  * Messages recieved by RabbitMQ are handled by standard Dotnet Core Controllers
  * Message handling is optionally tracked by Redis, enabling dependent message delivery


  Compare to:
	JasperFx: Jasper replaces the whole dotnet core MVC pipleine, where as this tries to work within it.
	EasyMQ: EasyMQ is an adapter purely for rabbitmq and also replaces the dotnet core MVC pipleine.

Why put so much effort into maintaining the dotnet core pipeline when they dont really do that much?  Most of the useful
applications for this system involve One-Way messaging
 * Most places start with HTTP based services and they have two concerns
 ** Want to invest the minimal amount of software development in migration to things like rabbitmq.
 ** Do not want to be locked into rabbitmq - the cost of migrating back should also be low.
 * MsgNThen uses all the standard classes from dotnet core, so for many vanilla systems the changes will be minimal.
 ** Controllers, MsgNThen uses standard Controllers.  The differences are just whether the controller needs to requesting msg redelivery or perhaps cancellation.  
 ** Routing, MsgNThen uses the standard router, so actually nothing changes in that regard.
 ** Authorization Filters, MsgNThen relies on the standard authorization pipeline.  The main difference is how the server delivers the failed responses.
 ** Non standard pipeline or use of specific http features.  Obviously systems like tls client certificates, or special use of http headers in the pipeline will need to be disabled.  But for the most part MgnNTHen and rabbitMQ should be able to similar replacements for these things.
 ** Program and Startup is where most changes are required, also consider that the configuration of rabbitmq exchange-queue binding is left to the implementation.  
 * Whilst the Controllers do not need to change much, the system that sends the request does need to change.  However, that can often be done with minimal effort.


 MsgNThen aims to be the simplest transition to queue based technologies as possible for dotnet core based APIs.  The motiviations to switch to queue based transports occur when
 * Fan out is required (multiple requests sent from the same sender)
 * Situations where sequences of interdependent http requests are being orchestrated in proc. (multiple requests sent from the same sender and then transformed into a single result)

 Why queues:
 * Using HTTP RPC is inefficient and complicated as the fan-out ratio gets higher.  RabbitMQ (or any MQ) lets the task distributor fire-and-forget the messages, and the queue length can be used to control scale-out.
 * Achieving good reliability when orchestrating multiple http connections becomes challenging in a variety of common situations, particularly when system load increases.  Conversely, fire-and-forget isolates concerns of reliability away from the business logic.



