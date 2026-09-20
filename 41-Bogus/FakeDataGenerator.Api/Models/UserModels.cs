namespace FakeDataGenerator.Api.Models;

public record UserAddress(
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country
);

public record UserProfile(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string PhoneNumber,
    string AvatarUrl,
    DateTime DateOfBirth,
    UserAddress Address,
    string CompanyName,
    string Role
);
