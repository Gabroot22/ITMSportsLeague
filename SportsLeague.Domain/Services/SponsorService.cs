using Microsoft.Extensions.Logging;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Interfaces.Repositories;
using SportsLeague.Domain.Interfaces.Services;

namespace SportsLeague.Domain.Services;

public class SponsorService : ISponsorService
{
    private readonly ISponsorRepository _sponsorRepo;
    private readonly ITournamentSponsorRepository _tournamentSponsorRepo;
    private readonly ITournamentRepository _tournamentRepo;
    private readonly ILogger<SponsorService> _logger;

    public SponsorService(
        ISponsorRepository sponsorRepo,
        ITournamentSponsorRepository tournamentSponsorRepo,
        ITournamentRepository tournamentRepo,
        ILogger<SponsorService> logger)
    {
        _sponsorRepo = sponsorRepo;
        _tournamentSponsorRepo = tournamentSponsorRepo;
        _tournamentRepo = tournamentRepo;
        _logger = logger;
    }

    public async Task<IEnumerable<Sponsor>> GetAllAsync()
    {
        _logger.LogInformation("Retrieving all sponsors");
        return await _sponsorRepo.GetAllAsync();
    }

    public async Task<Sponsor?> GetByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving sponsor with ID: {SponsorId}", id);
        var sponsor = await _sponsorRepo.GetByIdAsync(id);
        if (sponsor == null)
            _logger.LogWarning("Sponsor with ID {SponsorId} not found", id);
        return sponsor;
    }

    public async Task<Sponsor> CreateAsync(Sponsor sponsor)
    {
        if (await _sponsorRepo.ExistsByNameAsync(sponsor.Name))
        {
            _logger.LogWarning("Sponsor name '{Name}' already exists", sponsor.Name);
            throw new InvalidOperationException($"Ya existe un sponsor con el nombre '{sponsor.Name}'.");
        }

        if (!IsValidEmail(sponsor.ContactEmail))
        {
            _logger.LogWarning("Invalid email format: {Email}", sponsor.ContactEmail);
            throw new InvalidOperationException("El formato del email no es válido.");
        }

        sponsor.CreatedAt = DateTime.UtcNow;
        _logger.LogInformation("Creating sponsor: {Name}", sponsor.Name);
        return await _sponsorRepo.CreateAsync(sponsor);
    }

    public async Task UpdateAsync(int id, Sponsor sponsor)
    {
        var existing = await _sponsorRepo.GetByIdAsync(id);
        if (existing == null)
        {
            _logger.LogWarning("Sponsor with ID {SponsorId} not found", id);
            throw new KeyNotFoundException($"Sponsor con ID {id} no encontrado.");
        }

        var allSponsors = await _sponsorRepo.GetAllAsync();
        if (allSponsors.Any(s => s.Name.ToLower() == sponsor.Name.ToLower() && s.Id != id))
        {
            _logger.LogWarning("Sponsor name '{Name}' already exists", sponsor.Name);
            throw new InvalidOperationException($"Ya existe un sponsor con el nombre '{sponsor.Name}'.");
        }

        if (!IsValidEmail(sponsor.ContactEmail))
        {
            _logger.LogWarning("Invalid email format: {Email}", sponsor.ContactEmail);
            throw new InvalidOperationException("El formato del email no es válido.");
        }

        sponsor.UpdatedAt = DateTime.UtcNow;
        sponsor.Id = id;
        _logger.LogInformation("Updating sponsor with ID: {SponsorId}", id);
        await _sponsorRepo.UpdateAsync(sponsor);
    }

    public async Task DeleteAsync(int id)
    {
        var exists = await _sponsorRepo.ExistsAsync(id);
        if (!exists)
        {
            _logger.LogWarning("Sponsor with ID {SponsorId} not found", id);
            throw new KeyNotFoundException($"Sponsor con ID {id} no encontrado.");
        }
        _logger.LogInformation("Deleting sponsor with ID: {SponsorId}", id);
        await _sponsorRepo.DeleteAsync(id);
    }

    public async Task<TournamentSponsor> LinkSponsorToTournamentAsync(int sponsorId, int tournamentId, decimal contractAmount)
    {
        var sponsor = await _sponsorRepo.GetByIdAsync(sponsorId);
        if (sponsor == null)
        {
            _logger.LogWarning("Sponsor with ID {SponsorId} not found", sponsorId);
            throw new KeyNotFoundException($"Sponsor con ID {sponsorId} no encontrado.");
        }

        var tournament = await _tournamentRepo.GetByIdAsync(tournamentId);
        if (tournament == null)
        {
            _logger.LogWarning("Tournament with ID {TournamentId} not found", tournamentId);
            throw new KeyNotFoundException($"Tournament con ID {tournamentId} no encontrado.");
        }

        if (await _tournamentSponsorRepo.ExistsByTournamentAndSponsorAsync(tournamentId, sponsorId))
        {
            _logger.LogWarning("Sponsor {SponsorId} already linked to Tournament {TournamentId}", sponsorId, tournamentId);
            throw new InvalidOperationException("Este sponsor ya está vinculado a este torneo.");
        }

        if (contractAmount <= 0)
        {
            _logger.LogWarning("ContractAmount must be greater than 0");
            throw new InvalidOperationException("El ContractAmount debe ser mayor a 0.");
        }

        var tournamentSponsor = new TournamentSponsor
        {
            TournamentId = tournamentId,
            SponsorId = sponsorId,
            ContractAmount = contractAmount,
            JoinedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Linking Sponsor {SponsorId} to Tournament {TournamentId}", sponsorId, tournamentId);
        return await _tournamentSponsorRepo.CreateAsync(tournamentSponsor);
    }

    public async Task<IEnumerable<TournamentSponsor>> GetTournamentsBySponsorAsync(int sponsorId)
    {
        var sponsor = await _sponsorRepo.GetByIdAsync(sponsorId);
        if (sponsor == null)
        {
            _logger.LogWarning("Sponsor with ID {SponsorId} not found", sponsorId);
            throw new KeyNotFoundException($"Sponsor con ID {sponsorId} no encontrado.");
        }

        _logger.LogInformation("Retrieving tournaments for Sponsor {SponsorId}", sponsorId);
        return await _tournamentSponsorRepo.GetBySponsorIdAsync(sponsorId);
    }

    public async Task UnlinkSponsorFromTournamentAsync(int sponsorId, int tournamentId)
    {
        var tournamentSponsor = await _tournamentSponsorRepo.GetByTournamentAndSponsorAsync(tournamentId, sponsorId);
        if (tournamentSponsor == null)
        {
            _logger.LogWarning("Link between Sponsor {SponsorId} and Tournament {TournamentId} not found", sponsorId, tournamentId);
            throw new KeyNotFoundException($"No se encontró la vinculación entre Sponsor {sponsorId} y Tournament {tournamentId}.");
        }

        _logger.LogInformation("Unlinking Sponsor {SponsorId} from Tournament {TournamentId}", sponsorId, tournamentId);
        await _tournamentSponsorRepo.DeleteAsync(tournamentSponsor.Id);
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
