using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SportsLeague.API.DTOs.Request;
using SportsLeague.API.DTOs.Response;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Interfaces.Services;

namespace SportsLeague.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SponsorController : ControllerBase
{
    private readonly ISponsorService _sponsorService;
    private readonly IMapper _mapper;

    public SponsorController(ISponsorService sponsorService, IMapper mapper)
    {
        _sponsorService = sponsorService;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var sponsors = await _sponsorService.GetAllAsync();
        var dtos = _mapper.Map<IEnumerable<SponsorResponseDTO>>(sponsors);
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var sponsor = await _sponsorService.GetByIdAsync(id);
        if (sponsor == null)
            return NotFound($"Sponsor con ID {id} no encontrado.");

        var dto = _mapper.Map<SponsorResponseDTO>(sponsor);
        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SponsorRequestDTO dto)
    {
        try
        {
            var sponsor = _mapper.Map<Sponsor>(dto);
            var created = await _sponsorService.CreateAsync(sponsor);
            var responseDto = _mapper.Map<SponsorResponseDTO>(created);
            return CreatedAtAction(nameof(GetById), new { id = responseDto.Id }, responseDto);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] SponsorRequestDTO dto)
    {
        try
        {
            var sponsor = _mapper.Map<Sponsor>(dto);
            await _sponsorService.UpdateAsync(id, sponsor);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _sponsorService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("{id}/tournaments")]
    public async Task<IActionResult> GetTournamentsBySponsor(int id)
    {
        try
        {
            var tournamentSponsors = await _sponsorService.GetTournamentsBySponsorAsync(id);
            
            var responseList = new List<TournamentSponsorResponseDTO>();
            foreach (var ts in tournamentSponsors)
            {
                var dto = _mapper.Map<TournamentSponsorResponseDTO>(ts);
                dto.TournamentName = ts.Tournament?.Name;
                dto.SponsorName = ts.Sponsor?.Name;
                responseList.Add(dto);
            }

            return Ok(responseList);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("{id}/tournaments")]
    public async Task<IActionResult> LinkToTournament(int id, [FromBody] TournamentSponsorRequestDTO dto)
    {
        try
        {
            var tournamentSponsor = await _sponsorService.LinkSponsorToTournamentAsync(id, dto.TournamentId, dto.ContractAmount);
            
            var response = _mapper.Map<TournamentSponsorResponseDTO>(tournamentSponsor);
            response.TournamentName = tournamentSponsor.Tournament?.Name;
            response.SponsorName = tournamentSponsor.Sponsor?.Name;

            return CreatedAtAction(nameof(GetTournamentsBySponsor), new { id = response.SponsorId }, response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpDelete("{sponsorId}/tournaments/{tournamentId}")]
    public async Task<IActionResult> UnlinkFromTournament(int sponsorId, int tournamentId)
    {
        try
        {
            await _sponsorService.UnlinkSponsorFromTournamentAsync(sponsorId, tournamentId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
