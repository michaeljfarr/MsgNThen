# Test System Setup

docker pull rabbitmq
# 5672 is the rabbit port, 15672 is the management port
# this will come up on something like http://localhost:15672
docker run -d --hostname MsgNThenRabbitHost --name MsgNThenRabbit -e RABBITMQ_DEFAULT_USER=user -e RABBITMQ_DEFAULT_PASS=password -p 15671:15671 -p 15672:15672 -p 15691:15691 -p 15692:15692 -p 25672:25672 -p 4369:4369 -p 5671:5671 -p 5672:5672  rabbitmq:3-management
# or perhaps with less ports bound
docker run -d --hostname MsgNThenRabbitHost --name MsgNThenRabbit -e RABBITMQ_DEFAULT_USER=user -e RABBITMQ_DEFAULT_PASS=password -p 15672:15672 -p 5672:5672  rabbitmq:3-management


docker pull redis
docker run --name MsgNThenRedis -p 6379:6379 -v C:\Users\Micha\source\docker\redis:/data -d redis redis-server --appendonly yes