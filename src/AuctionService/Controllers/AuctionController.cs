using System.Reflection;
using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
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
    public AuctionController(AuctionDbContext auctionDbContext, IMapper mapper)
    {
        _auctionDbContext = auctionDbContext;
        _mapper = mapper;
        
    }

    [HttpGet]
    public async Task<ActionResult<List<AuctionDTO>>> GetAllAuctions()
    {
        var auctions = await _auctionDbContext.Auctions
                             .Include(x=>x.Item)
                             .OrderBy(x=>x.Item.Make)
                             .ToListAsync();
        return _mapper.Map<List<AuctionDTO>>(auctions);
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
        var result =  await _auctionDbContext.SaveChangesAsync() > 0;
        if(!result) return BadRequest("Could not connect to DB");
        return Ok();
    }
}