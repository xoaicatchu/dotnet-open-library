using Bogus;
using FakeDataGenerator.Api.Models;

namespace FakeDataGenerator.Api.Services;

public interface IFakeDataService
{
    List<UserProfile> GenerateUsers(int count, int? seed = null, string locale = "en");
    List<CustomerOrder> GenerateOrders(int count, int? seed = null);
    UserProfile GenerateCustomerById(Guid id);
}

public class FakeDataService : IFakeDataService
{
    public List<UserProfile> GenerateUsers(int count, int? seed = null, string locale = "en")
    {
        var addressFaker = CreateAddressFaker(locale, seed);
        var userFaker = CreateUserFaker(locale, addressFaker, seed);
        return userFaker.Generate(count);
    }

    public List<CustomerOrder> GenerateOrders(int count, int? seed = null)
    {
        var addressFaker = CreateAddressFaker("en", seed);
        var itemFaker = CreateOrderItemFaker(seed);
        var orderFaker = CreateOrderFaker(addressFaker, itemFaker, seed);
        return orderFaker.Generate(count);
    }

    public UserProfile GenerateCustomerById(Guid id)
    {
        // Use hash of GUID as deterministic seed so that the same ID always returns identical customer details
        int seed = id.GetHashCode();
        var addressFaker = CreateAddressFaker("en", seed);
        var userFaker = CreateUserFaker("en", addressFaker, seed)
            .RuleFor(u => u.Id, _ => id);

        return userFaker.Generate();
    }

    private static Faker<UserAddress> CreateAddressFaker(string locale, int? seed)
    {
        var faker = new Faker<UserAddress>(locale)
            .CustomInstantiator(f => new UserAddress(
                f.Address.StreetAddress(),
                f.Address.City(),
                f.Address.State(),
                f.Address.ZipCode(),
                f.Address.Country()
            ));

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker;
    }

    private static Faker<UserProfile> CreateUserFaker(string locale, Faker<UserAddress> addressFaker, int? seed)
    {
        var faker = new Faker<UserProfile>(locale)
            .CustomInstantiator(f =>
            {
                var firstName = f.Name.FirstName();
                var lastName = f.Name.LastName();
                var fullName = $"{firstName} {lastName}";
                var email = f.Internet.Email(firstName, lastName);
                var phone = f.Phone.PhoneNumber();
                var avatar = f.Internet.Avatar();
                var dob = f.Date.Past(40, DateTime.UtcNow.AddYears(-18));
                var address = addressFaker.Generate();
                var company = f.Company.CompanyName();
                var role = f.PickRandom("Admin", "Manager", "Developer", "Analyst", "Customer");

                return new UserProfile(
                    f.Random.Guid(),
                    firstName,
                    lastName,
                    fullName,
                    email,
                    phone,
                    avatar,
                    dob,
                    address,
                    company,
                    role
                );
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker;
    }

    private static Faker<OrderItem> CreateOrderItemFaker(int? seed)
    {
        var faker = new Faker<OrderItem>()
            .CustomInstantiator(f =>
            {
                var id = f.Random.Guid();
                var productId = f.Random.Guid();
                var productName = f.Commerce.ProductName();
                var unitPrice = Math.Round(f.Random.Decimal(5m, 500m), 2);
                var quantity = f.Random.Int(1, 10);
                var totalPrice = Math.Round(unitPrice * quantity, 2);

                return new OrderItem(id, productId, productName, unitPrice, quantity, totalPrice);
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker;
    }

    private static Faker<CustomerOrder> CreateOrderFaker(
        Faker<UserAddress> addressFaker,
        Faker<OrderItem> itemFaker,
        int? seed)
    {
        var faker = new Faker<CustomerOrder>()
            .CustomInstantiator(f =>
            {
                var id = f.Random.Guid();
                var orderNumber = $"ORD-{f.Date.Recent(30):yyyyMMdd}-{f.Random.AlphaNumeric(6).ToUpper()}";
                var customerId = f.Random.Guid();
                var customerName = f.Name.FullName();
                var customerEmail = f.Internet.Email(customerName);
                var orderDate = f.Date.Recent(60);
                var status = f.PickRandom("Pending", "Processing", "Shipped", "Delivered", "Cancelled");
                var items = itemFaker.Generate(f.Random.Int(1, 5));
                var totalAmount = items.Sum(i => i.TotalPrice);
                var address = addressFaker.Generate();

                return new CustomerOrder(
                    id,
                    orderNumber,
                    customerId,
                    customerName,
                    customerEmail,
                    orderDate,
                    status,
                    items,
                    totalAmount,
                    address
                );
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker;
    }
}
