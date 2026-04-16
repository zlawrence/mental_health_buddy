using AutoMapper;
using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;

namespace MentalHealthApp.Infrastructure.Services;

public class GuardRailService : IGuardRailService
{
    private readonly IGuardRailRepository _guardRailRepository;
    private readonly ITherapistAccessRepository _therapistAccessRepository;
    private readonly IMapper _mapper;

    public GuardRailService(
        IGuardRailRepository guardRailRepository,
        ITherapistAccessRepository therapistAccessRepository,
        IMapper mapper)
    {
        _guardRailRepository = guardRailRepository;
        _therapistAccessRepository = therapistAccessRepository;
        _mapper = mapper;
    }

    public async Task<GuardRailResponse> CreateGuardRailAsync(string therapistId, CreateGuardRailRequest request, CancellationToken cancellationToken = default)
    {
        // Verify therapist has access to patient and permission to manage guard rails
        var access = await _therapistAccessRepository.GetByTherapistAndPatientAsync(therapistId, request.PatientId, cancellationToken);
        if (access == null || !access.CanManageGuardRails || !access.IsActive)
        {
            throw new UnauthorizedAccessException("You don't have permission to manage guard rails for this patient");
        }

        var guardRail = _mapper.Map<GuardRail>(request);
        guardRail.TherapistUserId = therapistId;
        guardRail.IsActive = true;

        var created = await _guardRailRepository.AddAsync(guardRail, cancellationToken);
        return _mapper.Map<GuardRailResponse>(created);
    }

    public async Task<GuardRailResponse> UpdateGuardRailAsync(string guardRailId, string therapistId, UpdateGuardRailRequest request, CancellationToken cancellationToken = default)
    {
        var guardRail = await _guardRailRepository.GetByIdAsync(guardRailId, cancellationToken);
        if (guardRail == null || guardRail.TherapistUserId != therapistId)
        {
            throw new UnauthorizedAccessException("You don't have permission to update this guard rail");
        }

        guardRail.Keyword = request.Keyword;
        guardRail.Action = request.Action.Equals("remove", StringComparison.OrdinalIgnoreCase) 
            ? GuardRailAction.Remove 
            : GuardRailAction.Replace;
        guardRail.Replacement = request.Replacement;
        guardRail.IsActive = request.IsActive;
        guardRail.UpdatedAt = DateTime.UtcNow;

        await _guardRailRepository.UpdateAsync(guardRail, cancellationToken);
        return _mapper.Map<GuardRailResponse>(guardRail);
    }

    public async Task DeleteGuardRailAsync(string guardRailId, string therapistId, CancellationToken cancellationToken = default)
    {
        var guardRail = await _guardRailRepository.GetByIdAsync(guardRailId, cancellationToken);
        if (guardRail == null || guardRail.TherapistUserId != therapistId)
        {
            throw new UnauthorizedAccessException("You don't have permission to delete this guard rail");
        }

        await _guardRailRepository.DeleteAsync(guardRailId, cancellationToken);
    }

    public async Task<List<GuardRailResponse>> GetPatientGuardRailsAsync(string patientId, string therapistId, CancellationToken cancellationToken = default)
    {
        // Verify therapist has access to patient and permission to view guard rails
        var access = await _therapistAccessRepository.GetByTherapistAndPatientAsync(therapistId, patientId, cancellationToken);
        if (access == null || !access.CanManageGuardRails || !access.IsActive)
        {
            throw new UnauthorizedAccessException("You don't have permission to view guard rails for this patient");
        }

        var guardRails = await _guardRailRepository.GetByPatientAndTherapistAsync(patientId, therapistId, cancellationToken);
        return _mapper.Map<List<GuardRailResponse>>(guardRails);
    }
}
