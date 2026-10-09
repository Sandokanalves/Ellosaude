using AutoMapper;
using ElloSaude.Application.DTOs;
using ElloSaude.Domain.Entities;

namespace ElloSaude.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Patient, PatientDto>()
            .ForCtorParam(nameof(PatientDto.HasPortalAccount),
                options => options.MapFrom(source => source.UserId.HasValue));
        CreateMap<Appointment, AppointmentDto>();
        CreateMap<MedicalRecord, MedicalRecordDto>();
        CreateMap<Clinic, ClinicDto>();
    }
}
