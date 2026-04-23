using MentalHealthApp.Domain.Entities;
using NUnit.Framework;

namespace MentalHealthApp.Tests.Domain;

[TestFixture]
public class EntityDefaultsTests
{
    // ─── ConversationMessage ────────────────────────────────────────────────

    [Test]
    public void ConversationMessage_DefaultRole_IsUser()
    {
        var msg = new ConversationMessage();
        Assert.That(msg.Role, Is.EqualTo(MessageRole.User));
    }

    [Test]
    public void ConversationMessage_DefaultContent_IsEmpty()
    {
        var msg = new ConversationMessage();
        Assert.That(msg.Content, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ConversationMessage_DefaultConversationId_IsEmpty()
    {
        var msg = new ConversationMessage();
        Assert.That(msg.ConversationId, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ConversationMessage_DefaultId_IsNotEmpty()
    {
        var msg = new ConversationMessage();
        Assert.That(msg.Id, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void ConversationMessage_DefaultTimestamp_IsRecentUtc()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var msg = new ConversationMessage();
        Assert.That(msg.Timestamp, Is.GreaterThan(before));
    }

    [Test]
    public void ConversationMessage_AssistantRole_CanBeSet()
    {
        var msg = new ConversationMessage { Role = MessageRole.Assistant };
        Assert.That(msg.Role, Is.EqualTo(MessageRole.Assistant));
    }

    [Test]
    public void MessageRole_HasExpectedValues()
    {
        Assert.That(Enum.GetNames<MessageRole>(), Is.EquivalentTo(new[] { "User", "Assistant" }));
    }

    // ─── TherapistAccess ────────────────────────────────────────────────────

    [Test]
    public void TherapistAccess_DefaultCanViewChats_IsTrue()
    {
        var access = new TherapistAccess();
        Assert.That(access.CanViewChats, Is.True);
    }

    [Test]
    public void TherapistAccess_DefaultCanManageGuardRails_IsTrue()
    {
        var access = new TherapistAccess();
        Assert.That(access.CanManageGuardRails, Is.True);
    }

    [Test]
    public void TherapistAccess_DefaultIsActive_IsTrue()
    {
        var access = new TherapistAccess();
        Assert.That(access.IsActive, Is.True);
    }

    [Test]
    public void TherapistAccess_DefaultId_IsNotEmpty()
    {
        var access = new TherapistAccess();
        Assert.That(access.Id, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void TherapistAccess_PropertiesCanBeAssigned()
    {
        var access = new TherapistAccess
        {
            TherapistUserId = "therapist1",
            PatientUserId = "patient1",
            CanViewChats = false,
            CanManageGuardRails = false,
            IsActive = false
        };

        Assert.That(access.TherapistUserId, Is.EqualTo("therapist1"));
        Assert.That(access.PatientUserId, Is.EqualTo("patient1"));
        Assert.That(access.CanViewChats, Is.False);
        Assert.That(access.CanManageGuardRails, Is.False);
        Assert.That(access.IsActive, Is.False);
    }

    // ─── TherapistInvitation ────────────────────────────────────────────────

    [Test]
    public void TherapistInvitation_DefaultIsUsed_IsFalse()
    {
        var invite = new TherapistInvitation();
        Assert.That(invite.IsUsed, Is.False);
    }

    [Test]
    public void TherapistInvitation_DefaultUsedBy_IsNull()
    {
        var invite = new TherapistInvitation();
        Assert.That(invite.UsedBy, Is.Null);
    }

    [Test]
    public void TherapistInvitation_DefaultId_IsNotEmpty()
    {
        var invite = new TherapistInvitation();
        Assert.That(invite.Id, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void TherapistInvitation_PropertiesCanBeAssigned()
    {
        var expires = DateTime.UtcNow.AddDays(7);
        var invite = new TherapistInvitation
        {
            PatientUserId = "patient1",
            TherapistEmail = "therapist@example.com",
            Token = "some-token",
            ExpiresAt = expires,
            IsUsed = true,
            UsedBy = "therapist1"
        };

        Assert.That(invite.PatientUserId, Is.EqualTo("patient1"));
        Assert.That(invite.TherapistEmail, Is.EqualTo("therapist@example.com"));
        Assert.That(invite.ExpiresAt, Is.EqualTo(expires));
        Assert.That(invite.IsUsed, Is.True);
        Assert.That(invite.UsedBy, Is.EqualTo("therapist1"));
    }

    // ─── EmergencyContact ───────────────────────────────────────────────────

    [Test]
    public void EmergencyContact_DefaultIsPrimary_IsFalse()
    {
        var contact = new EmergencyContact();
        Assert.That(contact.IsPrimary, Is.False);
    }

    [Test]
    public void EmergencyContact_DefaultEmail_IsNull()
    {
        var contact = new EmergencyContact();
        Assert.That(contact.Email, Is.Null);
    }

    [Test]
    public void EmergencyContact_DefaultSmsNumber_IsNull()
    {
        var contact = new EmergencyContact();
        Assert.That(contact.SmsNumber, Is.Null);
    }

    [Test]
    public void EmergencyContact_DefaultName_IsEmpty()
    {
        var contact = new EmergencyContact();
        Assert.That(contact.Name, Is.EqualTo(string.Empty));
    }

    [Test]
    public void EmergencyContact_PropertiesCanBeAssigned()
    {
        var contact = new EmergencyContact
        {
            PatientUserId = "patient1",
            Name = "John Doe",
            Email = "john@example.com",
            SmsNumber = "+1234567890",
            IsPrimary = true
        };

        Assert.That(contact.PatientUserId, Is.EqualTo("patient1"));
        Assert.That(contact.Name, Is.EqualTo("John Doe"));
        Assert.That(contact.Email, Is.EqualTo("john@example.com"));
        Assert.That(contact.SmsNumber, Is.EqualTo("+1234567890"));
        Assert.That(contact.IsPrimary, Is.True);
    }

    // ─── MessageCount ───────────────────────────────────────────────────────

    [Test]
    public void MessageCount_DefaultCount_IsZero()
    {
        var mc = new MessageCount();
        Assert.That(mc.Count, Is.EqualTo(0));
    }

    [Test]
    public void MessageCount_DefaultPatientUserId_IsEmpty()
    {
        var mc = new MessageCount();
        Assert.That(mc.PatientUserId, Is.EqualTo(string.Empty));
    }

    [Test]
    public void MessageCount_DefaultId_IsNotEmpty()
    {
        var mc = new MessageCount();
        Assert.That(mc.Id, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void MessageCount_PropertiesCanBeAssigned()
    {
        var date = DateTime.UtcNow.Date;
        var mc = new MessageCount
        {
            PatientUserId = "patient1",
            Date = date,
            Count = 5
        };

        Assert.That(mc.PatientUserId, Is.EqualTo("patient1"));
        Assert.That(mc.Date, Is.EqualTo(date));
        Assert.That(mc.Count, Is.EqualTo(5));
    }

    // ─── AuditLog ───────────────────────────────────────────────────────────

    [Test]
    public void AuditLog_DefaultTargetId_IsNull()
    {
        var log = new AuditLog();
        Assert.That(log.TargetId, Is.Null);
    }

    [Test]
    public void AuditLog_DefaultTargetType_IsNull()
    {
        var log = new AuditLog();
        Assert.That(log.TargetType, Is.Null);
    }

    [Test]
    public void AuditLog_DefaultDetails_IsNull()
    {
        var log = new AuditLog();
        Assert.That(log.Details, Is.Null);
    }

    [Test]
    public void AuditLog_DefaultAdminId_IsEmpty()
    {
        var log = new AuditLog();
        Assert.That(log.AdminId, Is.EqualTo(string.Empty));
    }

    [Test]
    public void AuditLog_PropertiesCanBeAssigned()
    {
        var ts = DateTime.UtcNow;
        var log = new AuditLog
        {
            AdminId = "admin1",
            Action = "ResetPassword",
            TargetId = "user1",
            TargetType = "Patient",
            Details = "Reset via admin portal",
            Timestamp = ts
        };

        Assert.That(log.AdminId, Is.EqualTo("admin1"));
        Assert.That(log.Action, Is.EqualTo("ResetPassword"));
        Assert.That(log.TargetId, Is.EqualTo("user1"));
        Assert.That(log.TargetType, Is.EqualTo("Patient"));
        Assert.That(log.Details, Is.EqualTo("Reset via admin portal"));
        Assert.That(log.Timestamp, Is.EqualTo(ts));
    }

    // ─── Subscription ───────────────────────────────────────────────────────

    [Test]
    public void Subscription_DefaultStatus_IsInactive()
    {
        var sub = new Subscription();
        Assert.That(sub.Status, Is.EqualTo(SubscriptionStatus.Inactive));
    }

    [Test]
    public void Subscription_DefaultPurchaseDate_IsNull()
    {
        var sub = new Subscription();
        Assert.That(sub.PurchaseDate, Is.Null);
    }

    [Test]
    public void Subscription_DefaultRenewalDate_IsNull()
    {
        var sub = new Subscription();
        Assert.That(sub.RenewalDate, Is.Null);
    }

    [Test]
    public void Subscription_DefaultCanceledAt_IsNull()
    {
        var sub = new Subscription();
        Assert.That(sub.CanceledAt, Is.Null);
    }

    [Test]
    public void Subscription_DefaultStripeSubscriptionId_IsNull()
    {
        var sub = new Subscription();
        Assert.That(sub.StripeSubscriptionId, Is.Null);
    }

    [Test]
    public void Subscription_DefaultStripeCheckoutSessionId_IsNull()
    {
        var sub = new Subscription();
        Assert.That(sub.StripeCheckoutSessionId, Is.Null);
    }

    [Test]
    public void Subscription_PropertiesCanBeAssigned()
    {
        var now = DateTime.UtcNow;
        var sub = new Subscription
        {
            PatientUserId = "patient1",
            StripeCustomerId = "cus_abc",
            StripeSubscriptionId = "sub_abc",
            StripeCheckoutSessionId = "cs_abc",
            Status = SubscriptionStatus.Active,
            PurchaseDate = now,
            RenewalDate = now.AddMonths(1),
            CanceledAt = null
        };

        Assert.That(sub.PatientUserId, Is.EqualTo("patient1"));
        Assert.That(sub.StripeCustomerId, Is.EqualTo("cus_abc"));
        Assert.That(sub.Status, Is.EqualTo(SubscriptionStatus.Active));
        Assert.That(sub.PurchaseDate, Is.EqualTo(now));
        Assert.That(sub.RenewalDate, Is.EqualTo(now.AddMonths(1)));
        Assert.That(sub.CanceledAt, Is.Null);
    }

    [Test]
    public void SubscriptionStatus_HasExpectedValues()
    {
        var names = Enum.GetNames<SubscriptionStatus>();
        Assert.That(names, Is.EquivalentTo(new[] { "Inactive", "Pending", "Active", "Canceled", "PastDue" }));
    }

    [Test]
    public void Subscription_TwoInstances_HaveDifferentIds()
    {
        var a = new Subscription();
        var b = new Subscription();
        Assert.That(a.Id, Is.Not.EqualTo(b.Id));
    }
}
