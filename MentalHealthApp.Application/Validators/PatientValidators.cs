using FluentValidation;
using MentalHealthApp.Application.DTOs;

namespace MentalHealthApp.Application.Validators;

public class CreateGuardRailValidator : AbstractValidator<CreateGuardRailRequest>
{
    public CreateGuardRailValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Patient ID is required");

        RuleFor(x => x.Keyword)
            .NotEmpty().WithMessage("Keyword is required")
            .MaximumLength(100).WithMessage("Keyword must not exceed 100 characters");

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Action is required")
            .Must(x => x.Equals("remove", StringComparison.OrdinalIgnoreCase) || x.Equals("replace", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Action must be 'remove' or 'replace'");

        RuleFor(x => x.Replacement)
            .NotEmpty().When(x => x.Action.Equals("replace", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Replacement text is required when action is 'replace'")
            .MaximumLength(100).WithMessage("Replacement text must not exceed 100 characters");
    }
}

public class SendMessageValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty().WithMessage("Conversation ID is required");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required")
            .MaximumLength(2000).WithMessage("Message must not exceed 2000 characters");
    }
}

public class InviteTherapistValidator : AbstractValidator<InviteTherapistRequest>
{
    public InviteTherapistValidator()
    {
        RuleFor(x => x.TherapistEmail)
            .NotEmpty().WithMessage("Therapist email is required")
            .EmailAddress().WithMessage("Email must be valid");
    }
}

public class AddEmergencyContactValidator : AbstractValidator<AddEmergencyContactRequest>
{
    public AddEmergencyContactValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Contact name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Email must be valid");

        RuleFor(x => x.SmsNumber)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .When(x => !string.IsNullOrEmpty(x.SmsNumber))
            .WithMessage("Phone number must be valid E.164 format");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.Email) || !string.IsNullOrEmpty(x.SmsNumber))
            .WithMessage("At least one contact method (email or SMS) is required");
    }
}

public class CreateConversationValidator : AbstractValidator<CreateConversationRequest>
{
    public CreateConversationValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Conversation title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");
    }
}
