using Contracts;
using MassTransit;

namespace AuctionService.Consumers;

public class AuctionFaultCreatedConsumer : IConsumer<Fault<AuctionCreated>>
{
    public async Task Consume(ConsumeContext<Fault<AuctionCreated>> context)
    {
        Console.WriteLine("-----> Consuming Faulty Creation");
        var exception = context.Message.Exceptions.First();

        if(exception.ExceptionType == "System.ArgumentException")
        {
            context.Message.Message.Model = "FooBar";
            await context.Publish(context.Message.Message);
        }
        else
        {
            Console.WriteLine("-------> Not a Argument Exception");
        }
    }

}