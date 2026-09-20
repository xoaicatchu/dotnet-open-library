using System.Collections.Concurrent;
using ContactManager.Api.Models;

namespace ContactManager.Api.Data;

public class ContactStore
{
    private readonly ConcurrentDictionary<int, Contact> _contacts = new();
    private int _nextId = 1;

    public ContactStore()
    {
        var john = new Contact { Id = GetNextId(), FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        var jane = new Contact { Id = GetNextId(), FirstName = "Jane", LastName = "Smith", Email = "jane@example.com" };
        _contacts.TryAdd(john.Id, john);
        _contacts.TryAdd(jane.Id, jane);
    }

    private int GetNextId() => Interlocked.Increment(ref _nextId);

    public IEnumerable<Contact> GetAll() => _contacts.Values;

    public Contact? GetById(int id) => _contacts.TryGetValue(id, out var contact) ? contact : null;

    public Contact Add(Contact contact)
    {
        contact.Id = GetNextId();
        _contacts.TryAdd(contact.Id, contact);
        return contact;
    }

    public bool Update(Contact contact)
    {
        if (!_contacts.ContainsKey(contact.Id)) return false;
        _contacts[contact.Id] = contact;
        return true;
    }

    public bool Delete(int id)
    {
        return _contacts.TryRemove(id, out _);
    }
}
