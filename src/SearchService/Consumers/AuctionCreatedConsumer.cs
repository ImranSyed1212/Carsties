using MassTransit;
using Contracts;
using AutoMapper;
using Polly;
using SearchService.Models;
using MongoDB.Entities;

namespace SearchService.Consumers;

public class AuctionCreatedConsumer : IConsumer<AuctionCreated>
{
    private readonly IMapper _mapper;
    public AuctionCreatedConsumer(IMapper mapper)
    {
        _mapper = mapper;
    }

    public async Task Consume(ConsumeContext<AuctionCreated> consumeContext)
    {
        Console.WriteLine("Consuming Auction Created: " + consumeContext.Message.Id);

        var item = _mapper.Map<Item>(consumeContext.Message);
        if(item.Model == "Foo")
        {
            throw new ArgumentException("Cannot sell the car name Foo");
        }

        await item.SaveAsync();
    }
}