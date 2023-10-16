using System.Reflection;
using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace AuctionService.Controllers;

[ApiController()]
[Route("api/auctions")]
public class AuctionController : ControllerBase
{
    private readonly AuctionDbContext _auctionDbContext;
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint _publishEndpoint;
    public AuctionController(AuctionDbContext auctionDbContext, IMapper mapper, IPublishEndpoint publishEndpoint)
    {
        _auctionDbContext = auctionDbContext;
        _mapper = mapper;
        _publishEndpoint = publishEndpoint;
    }

    [HttpGet]
    public async Task<ActionResult<List<AuctionDTO>>> GetAllAuctions(string date)
    {
        var query = _auctionDbContext.Auctions.OrderBy(x=>x.Item.Make).AsQueryable();
        if(!string.IsNullOrEmpty(date))
        {
            query = query.Where(x=>x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime())>0);
        }       
        return await query.ProjectTo<AuctionDTO>(_mapper.ConfigurationProvider).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AuctionDTO>> GetAuctionById(Guid id)
    {
        var auction = await _auctionDbContext.Auctions
                            .Include(x=>x.Item)
                            .FirstOrDefaultAsync(x=>x.Id == id);
        if(auction == null) return NotFound();
        return _mapper.Map<AuctionDTO>(auction);
    }

    [HttpPost]
    public async Task<ActionResult<AuctionDTO>> CreateAuction(CreateAuctionDto createAuctionDto) 
    {
        var auction = _mapper.Map<Auction>(createAuctionDto);
        // TODO: Add current user as a seller
        auction.Seller = "test";

        _auctionDbContext.Auctions.Add(auction);
        var newAuction = _mapper.Map<AuctionDTO>(auction);
        
        await _publishEndpoint.Publish(_mapper.Map<AuctionCreated>(newAuction));
                
        var result = await _auctionDbContext.SaveChangesAsync() > 0;
    
        if(!result) return BadRequest("Could not save changes to the DB");

        return CreatedAtAction(nameof(GetAuctionById), new {auction.Id}, _mapper.Map<AuctionDTO>(auction));
    }

    [HttpPut]
    public async Task<ActionResult<AuctionDTO>> UpdateAction(Guid id,UpdateAuctionDto updateAuctionDto)
    {
        var auction = await _auctionDbContext.Auctions.Include(x => x.Item)
                            .FirstOrDefaultAsync();
        if(auction == null) return NotFound();

        //TODO : Check the seller = username

        auction.Item.Make =  updateAuctionDto.Make ?? auction.Item.Make;
        auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
        auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
        auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
        auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

        await _publishEndpoint.Publish(_mapper.Map<AuctionUpdated>(auction));

        var result = await _auctionDbContext.SaveChangesAsync() > 0 ;
        if(result) return Ok();
        return BadRequest("Problem while updating.....");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAuction(Guid id)
    {
        var acution = await _auctionDbContext.Auctions.FindAsync(id);
        if(acution == null) return NotFound();

        _auctionDbContext.Auctions.Remove(acution);

        await _publishEndpoint.Publish<AuctionDeleted>(new { Id = acution.Id.ToString() }); 
        
        var result =  await _auctionDbContext.SaveChangesAsync() > 0;
        if(!result) return BadRequest("Could not connect to DB");
        return Ok();
    }
}