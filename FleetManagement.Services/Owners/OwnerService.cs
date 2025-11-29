using FleetManagement.Data.Entities;
using FleetManagement.Data.Repositories;
using FleetManagement.Services.Abstractions;
using FleetManagement.Services.DTOs.Owners;
using FleetManagement.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Services.Owners;

public class OwnerService : IOwnerService
{
    private readonly IUnitOfWork _unitOfWork;

    public OwnerService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OwnerDetailResponse> GetAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        var owner = await LoadOwnerAsync(ownerId, cancellationToken);
        return Map(owner);
    }

    public async Task<OwnerDetailResponse> UpdateAsync(UpdateOwnerRequest request, CancellationToken cancellationToken = default)
    {
        var owner = await LoadOwnerAsync(request.OwnerId, cancellationToken);

        owner.ContactPhone = request.ContactPhone ?? owner.ContactPhone;
        owner.TimeZone = request.TimeZone ?? owner.TimeZone;

        if (request.CityId.HasValue && request.CityId != owner.CityId)
        {
            owner.CityId = request.CityId.Value;
        }

        _unitOfWork.Repository<Owner>().Update(owner);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        owner = await LoadOwnerAsync(owner.Id, cancellationToken);
        return Map(owner);
    }

    private async Task<Owner> LoadOwnerAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var owner = await _unitOfWork.Repository<Owner>()
            .Queryable
            .Include(o => o.City)
            .ThenInclude(city => city!.Country)
            .FirstOrDefaultAsync(o => o.Id == ownerId, cancellationToken);

        if (owner is null)
        {
            throw new NotFoundException(nameof(Owner), ownerId.ToString());
        }

        return owner;
    }

    private static OwnerDetailResponse Map(Owner owner) =>
        new()
        {
            Id = owner.Id,
            CompanyName = owner.CompanyName,
            ContactEmail = owner.ContactEmail,
            ContactPhone = owner.ContactPhone,
            City = owner.City?.Name,
            Country = owner.City?.Country?.Name,
            TimeZone = owner.TimeZone,
            FleetCount = owner.FleetCount,
            OktaGroupId = owner.OktaGroupId
        };
}

