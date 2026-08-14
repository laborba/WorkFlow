using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkFlow.Domain.Entities;

public class Tenant
{
    public long Id { get; private set; }

    public Guid PublicId { get; private set; }

    public string Name { get; private set; }

    public string RegistrationNumber { get; private set; }

    public string Email { get; private set; }

    public string? Phone { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }


    public Tenant(
        string name,
        string registrationNumber,
        string email,
        string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name cannot be empty.", nameof(name));

        if (string.IsNullOrWhiteSpace(registrationNumber))
            throw new ArgumentException("Registration number cannot be empty.", nameof(registrationNumber));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        PublicId = Guid.NewGuid();
        Name = name.Trim();
        RegistrationNumber = registrationNumber.Trim();
        Email = email.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name cannot be empty.", nameof(name));

        var normalizedName = name.Trim();

        if (Name == normalizedName)
            return;

        Name = normalizedName;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateContact(string email, string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        var normalizedEmail = email.Trim();

        var normalizedPhone = string.IsNullOrWhiteSpace(phone)
            ? null
            : phone.Trim();

        if (Email == normalizedEmail && Phone == normalizedPhone)
            return;

        Email = normalizedEmail;
        Phone = normalizedPhone;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}