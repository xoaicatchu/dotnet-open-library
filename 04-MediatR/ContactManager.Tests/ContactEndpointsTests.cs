using System.Net;
using System.Net.Http.Json;
using ContactManager.Api.Features.Contacts;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ContactManager.Tests;

public class ContactEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ContactEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetContacts_ReturnsSuccessAndDefaultContacts()
    {
        var response = await _client.GetAsync("/api/contacts");
        response.EnsureSuccessStatusCode();

        var contacts = await response.Content.ReadFromJsonAsync<List<ContactDto>>();
        Assert.NotNull(contacts);
        Assert.True(contacts.Count >= 2);
    }

    [Fact]
    public async Task SearchContacts_ReturnsMatchingContacts()
    {
        var response = await _client.GetAsync("/api/contacts?search=john");
        response.EnsureSuccessStatusCode();

        var contacts = await response.Content.ReadFromJsonAsync<List<ContactDto>>();
        Assert.NotNull(contacts);
        Assert.Contains(contacts, c => c.FirstName == "John");
        Assert.DoesNotContain(contacts, c => c.FirstName == "Jane");
    }

    [Fact]
    public async Task GetContactById_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/contacts/999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateContact_ValidationFails_Returns400()
    {
        var command = new CreateContactCommand("", "Test", "invalid-email", null, null);
        var response = await _client.PostAsJsonAsync("/api/contacts", command);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("errors", content);
        Assert.Contains("firstname", content);
        Assert.Contains("email", content);
    }

    [Fact]
    public async Task CrudOperations_RoundTrip()
    {
        // 1. Create
        var createCommand = new CreateContactCommand("Tom", "Jerry", "tom@jerry.com", "12345", "Cartoon");
        var createResponse = await _client.PostAsJsonAsync("/api/contacts", createCommand);
        createResponse.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var location = createResponse.Headers.Location?.ToString();
        Assert.NotNull(location);

        var createdContact = await createResponse.Content.ReadFromJsonAsync<ContactDto>();
        Assert.NotNull(createdContact);
        Assert.Equal("Tom", createdContact.FirstName);

        // 2. Read (GetById)
        var getResponse = await _client.GetAsync($"/api/contacts/{createdContact.Id}");
        getResponse.EnsureSuccessStatusCode();
        var fetchedContact = await getResponse.Content.ReadFromJsonAsync<ContactDto>();
        Assert.NotNull(fetchedContact);
        Assert.Equal("Tom", fetchedContact.FirstName);

        // 3. Update
        var updateRequest = new { FirstName = "Tommy", LastName = "Jerry", Email = "tommy@jerry.com", Phone = "12345", Company = "Cartoon" };
        var updateResponse = await _client.PutAsJsonAsync($"/api/contacts/{createdContact.Id}", updateRequest);
        updateResponse.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var getUpdatedResponse = await _client.GetAsync($"/api/contacts/{createdContact.Id}");
        var updatedContact = await getUpdatedResponse.Content.ReadFromJsonAsync<ContactDto>();
        Assert.Equal("Tommy", updatedContact?.FirstName);

        // 4. Delete
        var deleteResponse = await _client.DeleteAsync($"/api/contacts/{createdContact.Id}");
        deleteResponse.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // 5. Read again -> NotFound
        var getDeletedResponse = await _client.GetAsync($"/api/contacts/{createdContact.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    [Fact]
    public async Task SwaggerDevelopment_ReturnsSuccess()
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Environment", "Development");
        });
        
        var client = factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
    }
}
