using AutoMapper;
using ElloSaude.Application.DTOs;
using ElloSaude.Domain.Entities;

namespace ElloSaude.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Patient, PatientDto>();
        CreateMap<Appointment, AppointmentDto>();
        CreateMap<MedicalRecord, MedicalRecordDto>();
        CreateMap<Clinic, ClinicDto>();
    }
}
