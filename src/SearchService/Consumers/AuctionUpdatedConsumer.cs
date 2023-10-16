using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumers;

public class AuctionUpdatedConsumer : IConsumer<AuctionUpdated>
{
    private readonly IMapper _mapper;
    public AuctionUpdatedConsumer(IMapper mapper)
    {
        _mapper = mapper;
    }

    public async Task Consume(ConsumeContext<AuctionUpdated> consumeContext)
    {
        Console.WriteLine("--> Consuming auction updated: " + consumeContext.Message.Id);

        var item = _mapper.Map<Item>(consumeContext.Message);

        var result = await DB.Update<Item>()
                        .Match(a => a.ID == consumeContext.Message.Id)
                        .ModifyOnly(x => new
                            {
                                x.Make,
                                x.Model,
                                x.Year,
                                x.Mileage
                            }, item).ExecuteAsync();
        if (!result.IsAcknowledged) 
            throw new MessageException(typeof(AuctionUpdated), "Problem updating mongodb");
    }

}