using AutoMapper;
using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Domain.Entities;

namespace MentalHealthApp.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User
        CreateMap<User, PatientProfileResponse>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber));

        CreateMap<User, UserManagementResponse>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));

        CreateMap<RegisterPatientRequest, User>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username));

        // Conversation
        CreateMap<Conversation, ConversationResponse>();
        CreateMap<CreateConversationRequest, Conversation>();

        // ConversationMessage
        CreateMap<ConversationMessage, MessageResponse>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));

        // GuardRail
        CreateMap<GuardRail, GuardRailResponse>()
            .ForMember(dest => dest.Action, opt => opt.MapFrom(src => src.Action.ToString()));

        CreateMap<CreateGuardRailRequest, GuardRail>()
            .ForMember(dest => dest.Action, opt => opt.MapFrom(src => 
                src.Action.Equals("remove", StringComparison.OrdinalIgnoreCase) 
                    ? GuardRailAction.Remove 
                    : GuardRailAction.Replace));

        CreateMap<UpdateGuardRailRequest, GuardRail>()
            .ForMember(dest => dest.Action, opt => opt.MapFrom(src => 
                src.Action.Equals("remove", StringComparison.OrdinalIgnoreCase) 
                    ? GuardRailAction.Remove 
                    : GuardRailAction.Replace));

        // EmergencyContact
        CreateMap<EmergencyContact, EmergencyContactResponse>();
        CreateMap<AddEmergencyContactRequest, EmergencyContact>();

        // AuditLog
        CreateMap<AuditLog, AuditLogResponse>();

        // TherapistInvitation
        CreateMap<TherapistInvitation, TherapistInvitationResponse>();
    }
}
