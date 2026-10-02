using System.Net;
using System.Net.Http.Json;
using AspireDemo.ApiService.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AspireDemo.ApiService.Services;
using StackExchange.Redis;

namespace AspireDemo.Tests;

/// <summary>
/// Integration tests cho ApiService standalone (không cần Aspire AppHost chạy).
/// Dùng in-memory SQLite thay Postgres và mock IConnectionMultiplexer thay Redis.
/// </summary>
public class TasksApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TasksApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Thay Postgres bằng SQLite in-memory
                var dbDesc = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbDesc != null) services.Remove(dbDesc);

                services.AddDbContext<AppDbContext>(opts =>
                    opts.UseSqlite("Data Source=:memory:"));

                // Thay Redis bằng stub (ConnectionMultiplexer.Connect localhost cần server thật)
                var redisDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
                if (redisDesc != null) services.Remove(redisDesc);
                services.AddSingleton<IConnectionMultiplexer>(_ => new FakeConnectionMultiplexer());

                var queueDesc = services.SingleOrDefault(d => d.ServiceType == typeof(ITaskQueue));
                if (queueDesc != null) services.Remove(queueDesc);
                services.AddScoped<ITaskQueue, InMemoryTaskQueue>();
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetAll_EmptyDb_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tasks = await response.Content.ReadFromJsonAsync<List<TaskResponse>>();
        Assert.NotNull(tasks);
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201WithLocation()
    {
        var request = new CreateTaskRequest("Test task", "Description");

        var response = await _client.PostAsJsonAsync("/api/tasks", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.NotNull(task);
        Assert.Equal("Test task", task!.Title);
        Assert.Equal("Pending", task.Status);
        Assert.True(response.Headers.Location != null);
    }

    [Fact]
    public async Task Create_EmptyTitle_Returns400()
    {
        var request = new CreateTaskRequest("", "No title");

        var response = await _client.PostAsJsonAsync("/api/tasks", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        var response = await _client.GetAsync("/api/tasks/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateThenGetById_ReturnsSameTask()
    {
        var created = await _client.PostAsJsonAsync("/api/tasks",
            new CreateTaskRequest("Find me", "desc"));
        var task = await created.Content.ReadFromJsonAsync<TaskResponse>();

        var fetched = await _client.GetAsync($"/api/tasks/{task!.Id}");

        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        var fetchedTask = await fetched.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.Equal("Find me", fetchedTask!.Title);
    }

    [Fact]
    public async Task Delete_ExistingTask_Returns204()
    {
        var created = await _client.PostAsJsonAsync("/api/tasks",
            new CreateTaskRequest("Delete me", ""));
        var task = await created.Content.ReadFromJsonAsync<TaskResponse>();

        var delete = await _client.DeleteAsync($"/api/tasks/{task!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LivenessProbe_ReturnsOk()
    {
        var response = await _client.GetAsync("/alive");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task InfoEndpoint_ReturnsAspireFeatures()
    {
        var response = await _client.GetAsync("/api/info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("AspireDemo.ApiService", json);
        Assert.Contains("aspireFeatures", json);
    }
}

// ── Test doubles ───────────────────────────────────────────────────────────

/// <summary>In-memory task queue — không cần Redis trong unit tests.</summary>
public sealed class InMemoryTaskQueue : ITaskQueue
{
    private readonly Queue<int> _queue = new();
    public Task EnqueueAsync(int taskId, CancellationToken ct = default) { _queue.Enqueue(taskId); return Task.CompletedTask; }
    public Task<int?> DequeueAsync(CancellationToken ct = default) => Task.FromResult(_queue.Count > 0 ? (int?)_queue.Dequeue() : null);
}

/// <summary>
/// Minimal fake IConnectionMultiplexer — trả về no-op database.
/// Đủ để ApiService khởi động mà không cần Redis server thật.
/// </summary>
public sealed class FakeConnectionMultiplexer : IConnectionMultiplexer
{
    public string Configuration => "localhost:6379";
    public int TimeoutMilliseconds => 1000;
    public long OperationCount => 0;
    public bool PreserveAsyncOrder { get => false; set { } }
    public bool IsConnected => true;
    public bool IsConnecting => false;
    public event EventHandler<RedisErrorEventArgs>? ErrorMessage;
    public event EventHandler<ConnectionFailedEventArgs>? ConnectionFailed;
    public event EventHandler<InternalErrorEventArgs>? InternalError;
    public event EventHandler<ConnectionFailedEventArgs>? ConnectionRestored;
    public event EventHandler<EndPointEventArgs>? ConfigurationChanged;
    public event EventHandler<EndPointEventArgs>? ConfigurationChangedBroadcast;
    public event EventHandler<ServerMaintenanceEvent>? ServerMaintenanceEvent;
    public event EventHandler<HashSlotMovedEventArgs>? HashSlotMoved;

    public void Dispose() { }
    public void Close(bool allowCommandsToComplete = true) { }
    public Task CloseAsync(bool allowCommandsToComplete = true) => Task.CompletedTask;
    public bool Configure(System.IO.TextWriter? log = null) => true;
    public Task<bool> ConfigureAsync(System.IO.TextWriter? log = null) => Task.FromResult(true);
    public IDatabase GetDatabase(int db = -1, object? asyncState = null) => new FakeDatabase();
    public System.Net.EndPoint[] GetEndPoints(bool configuredOnly = false) => [];
    public IServer GetServer(string host, int port, object? asyncState = null) => throw new NotSupportedException();
    public IServer GetServer(string hostAndPort, object? asyncState = null) => throw new NotSupportedException();
    public IServer GetServer(System.Net.IPAddress host, int port) => throw new NotSupportedException();
    public IServer GetServer(System.Net.EndPoint endpoint, object? asyncState = null) => throw new NotSupportedException();
    public IServer[] GetServers() => [];
    public string GetStatus() => "fake";
    public void GetStatus(System.IO.TextWriter log) { }
    public ISubscriber GetSubscriber(object? asyncState = null) => throw new NotSupportedException();
    public int HashSlot(RedisKey key) => 0;
    public void RegisterProfiler(Func<ProfilingSession> profilingSessionProvider) { }
    public ServerCounters GetCounters() => new();
    public string ClientName => "fake";
    public Task<bool> TryConnectAsync(System.IO.TextWriter? log = null) => Task.FromResult(true);
    public Task<string> ExportConfigurationAsync(System.IO.TextWriter writer, ExportOptions options = 0) => Task.FromResult(string.Empty);
    public void ExportConfiguration(System.IO.TextWriter writer, ExportOptions options = 0) { }
    public void ResetStormLog() { }
    public string GetStormLog() => string.Empty;
    public void Wait(Task task) => task.Wait();
    public T Wait<T>(Task<T> task) => task.Result;
    public void WaitAll(params Task[] tasks) => Task.WaitAll(tasks);
    public bool IsConnected(RedisKey key, CommandFlags flags = 0) => true;
    public bool TryReconnect() => true;
}

/// <summary>No-op IDatabase — StringGetAsync returns empty so tests bypass Redis cache.</summary>
public sealed class FakeDatabase : IDatabase
{
    public int Database => 0;
    public IConnectionMultiplexer Multiplexer => null!;
    public IBatch CreateBatch(object? asyncState = null) => null!;
    public ITransaction CreateTransaction(object? asyncState = null) => null!;
    public void KeyMigrate(RedisKey key, System.Net.EndPoint toServer, int toDatabase = 0, int timeoutMilliseconds = 0, MigrateOptions migrateOptions = 0, CommandFlags flags = 0) { }
    public RedisValue DebugObject(RedisKey key, CommandFlags flags = 0) => default;
    public Task<RedisValue> DebugObjectAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult(RedisValue.EmptyString);
    public bool GeoAdd(RedisKey key, double longitude, double latitude, RedisValue member, CommandFlags flags = 0) => false;
    public bool GeoAdd(RedisKey key, GeoEntry value, CommandFlags flags = 0) => false;
    public long GeoAdd(RedisKey key, GeoEntry[] values, CommandFlags flags = 0) => 0;
    public Task<bool> GeoAddAsync(RedisKey key, double longitude, double latitude, RedisValue member, CommandFlags flags = 0) => Task.FromResult(false);
    public Task<bool> GeoAddAsync(RedisKey key, GeoEntry value, CommandFlags flags = 0) => Task.FromResult(false);
    public Task<long> GeoAddAsync(RedisKey key, GeoEntry[] values, CommandFlags flags = 0) => Task.FromResult(0L);
    public double? GeoDistance(RedisKey key, RedisValue member1, RedisValue member2, GeoUnit unit = 0, CommandFlags flags = 0) => null;
    public Task<double?> GeoDistanceAsync(RedisKey key, RedisValue member1, RedisValue member2, GeoUnit unit = 0, CommandFlags flags = 0) => Task.FromResult<double?>(null);
    public string?[] GeoHash(RedisKey key, RedisValue[] members, CommandFlags flags = 0) => [];
    public string? GeoHash(RedisKey key, RedisValue member, CommandFlags flags = 0) => null;
    public Task<string?[]> GeoHashAsync(RedisKey key, RedisValue[] members, CommandFlags flags = 0) => Task.FromResult<string?[]>([]);
    public Task<string?> GeoHashAsync(RedisKey key, RedisValue member, CommandFlags flags = 0) => Task.FromResult<string?>(null);
    public GeoPosition?[] GeoPosition(RedisKey key, RedisValue[] members, CommandFlags flags = 0) => [];
    public GeoPosition? GeoPosition(RedisKey key, RedisValue member, CommandFlags flags = 0) => null;
    public Task<GeoPosition?[]> GeoPositionAsync(RedisKey key, RedisValue[] members, CommandFlags flags = 0) => Task.FromResult<GeoPosition?[]>([]);
    public Task<GeoPosition?> GeoPositionAsync(RedisKey key, RedisValue member, CommandFlags flags = 0) => Task.FromResult<GeoPosition?>(null);
    public GeoRadiusResult[] GeoRadius(RedisKey key, RedisValue member, double radius, GeoUnit unit = 0, int count = -1, Order? order = null, GeoRadiusOptions options = 0, CommandFlags flags = 0) => [];
    public GeoRadiusResult[] GeoRadius(RedisKey key, double longitude, double latitude, double radius, GeoUnit unit = 0, int count = -1, Order? order = null, GeoRadiusOptions options = 0, CommandFlags flags = 0) => [];
    public Task<GeoRadiusResult[]> GeoRadiusAsync(RedisKey key, RedisValue member, double radius, GeoUnit unit = 0, int count = -1, Order? order = null, GeoRadiusOptions options = 0, CommandFlags flags = 0) => Task.FromResult<GeoRadiusResult[]>([]);
    public Task<GeoRadiusResult[]> GeoRadiusAsync(RedisKey key, double longitude, double latitude, double radius, GeoUnit unit = 0, int count = -1, Order? order = null, GeoRadiusOptions options = 0, CommandFlags flags = 0) => Task.FromResult<GeoRadiusResult[]>([]);
    public bool GeoRemove(RedisKey key, RedisValue member, CommandFlags flags = 0) => false;
    public Task<bool> GeoRemoveAsync(RedisKey key, RedisValue member, CommandFlags flags = 0) => Task.FromResult(false);
    public GeoSearchResult[] GeoSearch(RedisKey key, RedisValue member, GeoSearchShape shape, int count = -1, bool demandClosest = true, Order? order = null, GeoRadiusOptions options = 0, CommandFlags flags = 0) => [];
    public GeoSearchResult[] GeoSearch(RedisKey key, double longitude, double latitude, GeoSearchShape shape, int count = -1, bool demandClosest = true, Order? order = null, GeoRadiusOptions options = 0, CommandFlags flags = 0) => [];
    public Task<GeoSearchResult[]> GeoSearchAsync(RedisKey key, RedisValue member, GeoSearchShape shape, int count = -1, bool demandClosest = true, Order? order = null, GeoRadiusOptions options = 0, CommandFlags flags = 0) => Task.FromResult<GeoSearchResult[]>([]);
    public Task<GeoSearchResult[]> GeoSearchAsync(RedisKey key, double longitude, double latitude, GeoSearchShape shape, int count = -1, bool demandClosest = true, Order? order = null, GeoRadiusOptions options = 0, CommandFlags flags = 0) => Task.FromResult<GeoSearchResult[]>([]);
    public long GeoSearchAndStore(RedisKey sourceKey, RedisKey destinationKey, RedisValue member, GeoSearchShape shape, int count = -1, bool demandClosest = true, Order? order = null, bool storeDistances = false, CommandFlags flags = 0) => 0;
    public long GeoSearchAndStore(RedisKey sourceKey, RedisKey destinationKey, double longitude, double latitude, GeoSearchShape shape, int count = -1, bool demandClosest = true, Order? order = null, bool storeDistances = false, CommandFlags flags = 0) => 0;
    public Task<long> GeoSearchAndStoreAsync(RedisKey sourceKey, RedisKey destinationKey, RedisValue member, GeoSearchShape shape, int count = -1, bool demandClosest = true, Order? order = null, bool storeDistances = false, CommandFlags flags = 0) => Task.FromResult(0L);
    public Task<long> GeoSearchAndStoreAsync(RedisKey sourceKey, RedisKey destinationKey, double longitude, double latitude, GeoSearchShape shape, int count = -1, bool demandClosest = true, Order? order = null, bool storeDistances = false, CommandFlags flags = 0) => Task.FromResult(0L);
    public long HashDecrement(RedisKey key, RedisValue hashField, long value = 1, CommandFlags flags = 0) => 0;
    public double HashDecrement(RedisKey key, RedisValue hashField, double value, CommandFlags flags = 0) => 0;
    public Task<long> HashDecrementAsync(RedisKey key, RedisValue hashField, long value = 1, CommandFlags flags = 0) => Task.FromResult(0L);
    public Task<double> HashDecrementAsync(RedisKey key, RedisValue hashField, double value, CommandFlags flags = 0) => Task.FromResult(0.0);
    public bool HashDelete(RedisKey key, RedisValue hashField, CommandFlags flags = 0) => false;
    public long HashDelete(RedisKey key, RedisValue[] hashFields, CommandFlags flags = 0) => 0;
    public Task<bool> HashDeleteAsync(RedisKey key, RedisValue hashField, CommandFlags flags = 0) => Task.FromResult(false);
    public Task<long> HashDeleteAsync(RedisKey key, RedisValue[] hashFields, CommandFlags flags = 0) => Task.FromResult(0L);
    public bool HashExists(RedisKey key, RedisValue hashField, CommandFlags flags = 0) => false;
    public Task<bool> HashExistsAsync(RedisKey key, RedisValue hashField, CommandFlags flags = 0) => Task.FromResult(false);
    public HashEntry[] HashGetAll(RedisKey key, CommandFlags flags = 0) => [];
    public Task<HashEntry[]> HashGetAllAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult<HashEntry[]>([]);
    public RedisValue HashGet(RedisKey key, RedisValue hashField, CommandFlags flags = 0) => default;
    public RedisValue[] HashGet(RedisKey key, RedisValue[] hashFields, CommandFlags flags = 0) => [];
    public Lease<byte>? HashGetLease(RedisKey key, RedisValue hashField, CommandFlags flags = 0) => null;
    public Task<Lease<byte>?> HashGetLeaseAsync(RedisKey key, RedisValue hashField, CommandFlags flags = 0) => Task.FromResult<Lease<byte>?>(null);
    public Task<RedisValue> HashGetAsync(RedisKey key, RedisValue hashField, CommandFlags flags = 0) => Task.FromResult(RedisValue.Null);
    public Task<RedisValue[]> HashGetAsync(RedisKey key, RedisValue[] hashFields, CommandFlags flags = 0) => Task.FromResult<RedisValue[]>([]);
    public long HashIncrement(RedisKey key, RedisValue hashField, long value = 1, CommandFlags flags = 0) => 0;
    public double HashIncrement(RedisKey key, RedisValue hashField, double value, CommandFlags flags = 0) => 0;
    public Task<long> HashIncrementAsync(RedisKey key, RedisValue hashField, long value = 1, CommandFlags flags = 0) => Task.FromResult(0L);
    public Task<double> HashIncrementAsync(RedisKey key, RedisValue hashField, double value, CommandFlags flags = 0) => Task.FromResult(0.0);
    public IEnumerable<HashEntry> HashScan(RedisKey key, RedisValue pattern = default, int pageSize = 250, long cursor = 0, int pageOffset = 0, CommandFlags flags = 0) => [];
    public IAsyncEnumerable<HashEntry> HashScanAsync(RedisKey key, RedisValue pattern = default, int pageSize = 250, long cursor = 0, int pageOffset = 0, CommandFlags flags = 0) => AsyncEnumerable.Empty<HashEntry>();
    public RedisValue[] HashKeys(RedisKey key, CommandFlags flags = 0) => [];
    public Task<RedisValue[]> HashKeysAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult<RedisValue[]>([]);
    public long HashLength(RedisKey key, CommandFlags flags = 0) => 0;
    public Task<long> HashLengthAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult(0L);
    public void HashSet(RedisKey key, HashEntry[] hashFields, CommandFlags flags = 0) { }
    public bool HashSet(RedisKey key, RedisValue hashField, RedisValue value, When when = 0, CommandFlags flags = 0) => false;
    public Task HashSetAsync(RedisKey key, HashEntry[] hashFields, CommandFlags flags = 0) => Task.CompletedTask;
    public Task<bool> HashSetAsync(RedisKey key, RedisValue hashField, RedisValue value, When when = 0, CommandFlags flags = 0) => Task.FromResult(false);
    public long HashStringLength(RedisKey key, RedisValue hashField, CommandFlags flags = 0) => 0;
    public Task<long> HashStringLengthAsync(RedisKey key, RedisValue hashField, CommandFlags flags = 0) => Task.FromResult(0L);
    public RedisValue[] HashValues(RedisKey key, CommandFlags flags = 0) => [];
    public Task<RedisValue[]> HashValuesAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult<RedisValue[]>([]);
    public long HyperLogLogAdd(RedisKey key, RedisValue value, CommandFlags flags = 0) => 0;
    public long HyperLogLogAdd(RedisKey key, RedisValue[] values, CommandFlags flags = 0) => 0;
    public Task<long> HyperLogLogAddAsync(RedisKey key, RedisValue value, CommandFlags flags = 0) => Task.FromResult(0L);
    public Task<long> HyperLogLogAddAsync(RedisKey key, RedisValue[] values, CommandFlags flags = 0) => Task.FromResult(0L);
    public long HyperLogLogLength(RedisKey key, CommandFlags flags = 0) => 0;
    public long HyperLogLogLength(RedisKey[] keys, CommandFlags flags = 0) => 0;
    public Task<long> HyperLogLogLengthAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult(0L);
    public Task<long> HyperLogLogLengthAsync(RedisKey[] keys, CommandFlags flags = 0) => Task.FromResult(0L);
    public void HyperLogLogMerge(RedisKey destination, RedisKey first, RedisKey second, CommandFlags flags = 0) { }
    public void HyperLogLogMerge(RedisKey destination, RedisKey[] sourceKeys, CommandFlags flags = 0) { }
    public Task HyperLogLogMergeAsync(RedisKey destination, RedisKey first, RedisKey second, CommandFlags flags = 0) => Task.CompletedTask;
    public Task HyperLogLogMergeAsync(RedisKey destination, RedisKey[] sourceKeys, CommandFlags flags = 0) => Task.CompletedTask;
    public System.Net.EndPoint? IdentifyEndpoint(RedisKey key = default, CommandFlags flags = 0) => null;
    public Task<System.Net.EndPoint?> IdentifyEndpointAsync(RedisKey key = default, CommandFlags flags = 0) => Task.FromResult<System.Net.EndPoint?>(null);
    public bool IsConnected(RedisKey key, CommandFlags flags = 0) => true;
    public bool KeyCopy(RedisKey sourceKey, RedisKey destinationKey, int destinationDatabase = -1, bool replace = false, CommandFlags flags = 0) => false;
    public Task<bool> KeyCopyAsync(RedisKey sourceKey, RedisKey destinationKey, int destinationDatabase = -1, bool replace = false, CommandFlags flags = 0) => Task.FromResult(false);
    public bool KeyDelete(RedisKey key, CommandFlags flags = 0) => false;
    public long KeyDelete(RedisKey[] keys, CommandFlags flags = 0) => 0;
    public Task<bool> KeyDeleteAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult(false);
    public Task<long> KeyDeleteAsync(RedisKey[] keys, CommandFlags flags = 0) => Task.FromResult(0L);
    public byte[]? KeyDump(RedisKey key, CommandFlags flags = 0) => null;
    public Task<byte[]?> KeyDumpAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult<byte[]?>(null);
    public string? KeyEncoding(RedisKey key, CommandFlags flags = 0) => null;
    public Task<string?> KeyEncodingAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult<string?>(null);
    public bool KeyExists(RedisKey key, CommandFlags flags = 0) => false;
    public long KeyExists(RedisKey[] keys, CommandFlags flags = 0) => 0;
    public Task<bool> KeyExistsAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult(false);
    public Task<long> KeyExistsAsync(RedisKey[] keys, CommandFlags flags = 0) => Task.FromResult(0L);
    public bool KeyExpire(RedisKey key, TimeSpan? expiry, CommandFlags flags = 0) => false;
    public bool KeyExpire(RedisKey key, DateTime? expiry, CommandFlags flags = 0) => false;
    public bool KeyExpire(RedisKey key, TimeSpan? expiry, ExpireWhen when, CommandFlags flags = 0) => false;
    public bool KeyExpire(RedisKey key, DateTime? expiry, ExpireWhen when, CommandFlags flags = 0) => false;
    public Task<bool> KeyExpireAsync(RedisKey key, TimeSpan? expiry, CommandFlags flags = 0) => Task.FromResult(false);
    public Task<bool> KeyExpireAsync(RedisKey key, DateTime? expiry, CommandFlags flags = 0) => Task.FromResult(false);
    public Task<bool> KeyExpireAsync(RedisKey key, TimeSpan? expiry, ExpireWhen when, CommandFlags flags = 0) => Task.FromResult(false);
    public Task<bool> KeyExpireAsync(RedisKey key, DateTime? expiry, ExpireWhen when, CommandFlags flags = 0) => Task.FromResult(false);
    public DateTime? KeyExpireTime(RedisKey key, CommandFlags flags = 0) => null;
    public Task<DateTime?> KeyExpireTimeAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult<DateTime?>(null);
    public long? KeyFrequency(RedisKey key, CommandFlags flags = 0) => null;
    public Task<long?> KeyFrequencyAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult<long?>(null);
    public TimeSpan? KeyIdleTime(RedisKey key, CommandFlags flags = 0) => null;
    public Task<TimeSpan?> KeyIdleTimeAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult<TimeSpan?>(null);
    public void KeyMigrate(RedisKey key, System.Net.EndPoint toServer, int toDatabase = 0, int timeoutMilliseconds = 0, MigrateOptions migrateOptions = 0, CommandFlags flags = 0) { }
    public Task KeyMigrateAsync(RedisKey key, System.Net.EndPoint toServer, int toDatabase = 0, int timeoutMilliseconds = 0, MigrateOptions migrateOptions = 0, CommandFlags flags = 0) => Task.CompletedTask;
    public bool KeyMove(RedisKey key, int database, CommandFlags flags = 0) => false;
    public Task<bool> KeyMoveAsync(RedisKey key, int database, CommandFlags flags = 0) => Task.FromResult(false);
    public bool KeyPersist(RedisKey key, CommandFlags flags = 0) => false;
    public Task<bool> KeyPersistAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult(false);
    public RedisKey KeyRandom(CommandFlags flags = 0) => default;
    public Task<RedisKey> KeyRandomAsync(CommandFlags flags = 0) => Task.FromResult(RedisKey.Empty);
    public long? KeyRefCount(RedisKey key, CommandFlags flags = 0) => null;
    public Task<long?> KeyRefCountAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult<long?>(null);
    public bool KeyRename(RedisKey key, RedisKey newKey, When when = 0, CommandFlags flags = 0) => false;
    public Task<bool> KeyRenameAsync(RedisKey key, RedisKey newKey, When when = 0, CommandFlags flags = 0) => Task.FromResult(false);
    public void KeyRestore(RedisKey key, byte[] value, TimeSpan? expiry = null, CommandFlags flags = 0) { }
    public Task KeyRestoreAsync(RedisKey key, byte[] value, TimeSpan? expiry = null, CommandFlags flags = 0) => Task.CompletedTask;
    public IEnumerable<RedisKey> KeyScan(RedisKey pattern = default, int pageSize = 250, long cursor = 0, int pageOffset = 0, CommandFlags flags = 0) => [];
    public IAsyncEnumerable<RedisKey> KeyScanAsync(RedisKey pattern = default, int pageSize = 250, long cursor = 0, int pageOffset = 0, CommandFlags flags = 0) => AsyncEnumerable.Empty<RedisKey>();
    public TimeSpan? KeyTimeToLive(RedisKey key, CommandFlags flags = 0) => null;
    public Task<TimeSpan?> KeyTimeToLiveAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult<TimeSpan?>(null);
    public RedisType KeyType(RedisKey key, CommandFlags flags = 0) => RedisType.None;
    public Task<RedisType> KeyTypeAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult(RedisType.None);
    public RedisValue ListGetByIndex(RedisKey key, long index, CommandFlags flags = 0) => default;
    public Task<RedisValue> ListGetByIndexAsync(RedisKey key, long index, CommandFlags flags = 0) => Task.FromResult(RedisValue.Null);
    public long ListInsertAfter(RedisKey key, RedisValue pivot, RedisValue value, CommandFlags flags = 0) => 0;
    public long ListInsertBefore(RedisKey key, RedisValue pivot, RedisValue value, CommandFlags flags = 0) => 0;
    public Task<long> ListInsertAfterAsync(RedisKey key, RedisValue pivot, RedisValue value, CommandFlags flags = 0) => Task.FromResult(0L);
    public Task<long> ListInsertBeforeAsync(RedisKey key, RedisValue pivot, RedisValue value, CommandFlags flags = 0) => Task.FromResult(0L);
    public RedisValue ListLeftPop(RedisKey key, CommandFlags flags = 0) => default;
    public RedisValue[] ListLeftPop(RedisKey key, long count, CommandFlags flags = 0) => [];
    public ListPopResult ListLeftPop(RedisKey[] keys, long count, CommandFlags flags = 0) => default;
    public Task<RedisValue> ListLeftPopAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult(RedisValue.Null);
    public Task<RedisValue[]> ListLeftPopAsync(RedisKey key, long count, CommandFlags flags = 0) => Task.FromResult<RedisValue[]>([]);
    public Task<ListPopResult> ListLeftPopAsync(RedisKey[] keys, long count, CommandFlags flags = 0) => Task.FromResult(default(ListPopResult));
    public long ListLeftPush(RedisKey key, RedisValue value, When when = 0, CommandFlags flags = 0) => 0;
    public long ListLeftPush(RedisKey key, RedisValue[] values, When when = 0, CommandFlags flags = 0) => 0;
    public Task<long> ListLeftPushAsync(RedisKey key, RedisValue value, When when = 0, CommandFlags flags = 0) => Task.FromResult(0L);
    public Task<long> ListLeftPushAsync(RedisKey key, RedisValue[] values, When when = 0, CommandFlags flags = 0) => Task.FromResult(0L);
    public long ListLength(RedisKey key, CommandFlags flags = 0) => 0;
    public Task<long> ListLengthAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult(0L);
    public RedisKey ListMove(RedisKey sourceKey, RedisKey destinationKey, ListSide sourceSide, ListSide destinationSide, CommandFlags flags = 0) => default;
    public Task<RedisKey> ListMoveAsync(RedisKey sourceKey, RedisKey destinationKey, ListSide sourceSide, ListSide destinationSide, CommandFlags flags = 0) => Task.FromResult(RedisKey.Empty);
    public IEnumerable<RedisValue> ListRange(RedisKey key, long start = 0, long stop = -1, CommandFlags flags = 0) => [];
    public Task<RedisValue[]> ListRangeAsync(RedisKey key, long start = 0, long stop = -1, CommandFlags flags = 0) => Task.FromResult<RedisValue[]>([]);
    public long ListRemove(RedisKey key, RedisValue value, long count = 0, CommandFlags flags = 0) => 0;
    public Task<long> ListRemoveAsync(RedisKey key, RedisValue value, long count = 0, CommandFlags flags = 0) => Task.FromResult(0L);
    public RedisValue ListRightPop(RedisKey key, CommandFlags flags = 0) => default;
    public RedisValue[] ListRightPop(RedisKey key, long count, CommandFlags flags = 0) => [];
    public ListPopResult ListRightPop(RedisKey[] keys, long count, CommandFlags flags = 0) => default;
    public Task<RedisValue> ListRightPopAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult(RedisValue.Null);
    public Task<RedisValue[]> ListRightPopAsync(RedisKey key, long count, CommandFlags flags = 0) => Task.FromResult<RedisValue[]>([]);
    public Task<ListPopResult> ListRightPopAsync(RedisKey[] keys, long count, CommandFlags flags = 0) => Task.FromResult(default(ListPopResult));
    public RedisValue ListRightPopLeftPush(RedisKey source, RedisKey destination, CommandFlags flags = 0) => default;
    public Task<RedisValue> ListRightPopLeftPushAsync(RedisKey source, RedisKey destination, CommandFlags flags = 0) => Task.FromResult(RedisValue.Null);
    public long ListRightPush(RedisKey key, RedisValue value, When when = 0, CommandFlags flags = 0) => 0;
    public long ListRightPush(RedisKey key, RedisValue[] values, When when = 0, CommandFlags flags = 0) => 0;
    public Task<long> ListRightPushAsync(RedisKey key, RedisValue value, When when = 0, CommandFlags flags = 0) => Task.FromResult(0L);
    public Task<long> ListRightPushAsync(RedisKey key, RedisValue[] values, When when = 0, CommandFlags flags = 0) => Task.FromResult(0L);
    public void ListSetByIndex(RedisKey key, long index, RedisValue value, CommandFlags flags = 0) { }
    public Task ListSetByIndexAsync(RedisKey key, long index, RedisValue value, CommandFlags flags = 0) => Task.CompletedTask;
    public void ListTrim(RedisKey key, long start, long stop, CommandFlags flags = 0) { }
    public Task ListTrimAsync(RedisKey key, long start, long stop, CommandFlags flags = 0) => Task.CompletedTask;
    public bool LockExtend(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = 0) => false;
    public Task<bool> LockExtendAsync(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = 0) => Task.FromResult(false);
    public RedisValue LockQuery(RedisKey key, CommandFlags flags = 0) => default;
    public Task<RedisValue> LockQueryAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult(RedisValue.Null);
    public bool LockRelease(RedisKey key, RedisValue value, CommandFlags flags = 0) => false;
    public Task<bool> LockReleaseAsync(RedisKey key, RedisValue value, CommandFlags flags = 0) => Task.FromResult(false);
    public bool LockTake(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = 0) => false;
    public Task<bool> LockTakeAsync(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = 0) => Task.FromResult(false);
    public TimeSpan Ping(CommandFlags flags = 0) => TimeSpan.Zero;
    public Task<TimeSpan> PingAsync(CommandFlags flags = 0) => Task.FromResult(TimeSpan.Zero);
    public long Publish(RedisChannel channel, RedisValue message, CommandFlags flags = 0) => 0;
    public Task<long> PublishAsync(RedisChannel channel, RedisValue message, CommandFlags flags = 0) => Task.FromResult(0L);
    public RedisResult Execute(string command, params object[] args) => RedisResult.Create(RedisValue.Null);
    public RedisResult Execute(string command, ICollection<object>? args, CommandFlags flags = 0) => RedisResult.Create(RedisValue.Null);
    public Task<RedisResult> ExecuteAsync(string command, params object[] args) => Task.FromResult(RedisResult.Create(RedisValue.Null));
    public Task<RedisResult> ExecuteAsync(string command, ICollection<object>? args, CommandFlags flags = 0) => Task.FromResult(RedisResult.Create(RedisValue.Null));
    public bool SetAdd(RedisKey key, RedisValue value, CommandFlags flags = 0) => false;
    public long SetAdd(RedisKey key, RedisValue[] values, CommandFlags flags = 0) => 0;
    public Task<bool> SetAddAsync(RedisKey key, RedisValue value, CommandFlags flags = 0) => Task.FromResult(false);
    public Task<long> SetAddAsync(RedisKey key, RedisValue[] values, CommandFlags flags = 0) => Task.FromResult(0L);
    public RedisValue[] SetCombine(SetOperation operation, RedisKey first, RedisKey second, CommandFlags flags = 0) => [];
    public RedisValue[] SetCombine(SetOperation operation, RedisKey[] keys, CommandFlags flags = 0) => [];
    public long SetCombineAndStore(SetOperation operation, RedisKey destination, RedisKey first, RedisKey second, CommandFlags flags = 0) => 0;
    public long SetCombineAndStore(SetOperation operation, RedisKey destination, RedisKey[] keys, CommandFlags flags = 0) => 0;
    public Task<RedisValue[]> SetCombineAsync(SetOperation operation, RedisKey first, RedisKey second, CommandFlags flags = 0) => Task.FromResult<RedisValue[]>([]);
    public Task<RedisValue[]> SetCombineAsync(SetOperation operation, RedisKey[] keys, CommandFlags flags = 0) => Task.FromResult<RedisValue[]>([]);
    public Task<long> SetCombineAndStoreAsync(SetOperation operation, RedisKey destination, RedisKey first, RedisKey second, CommandFlags flags = 0) => Task.FromResult(0L);
    public Task<long> SetCombineAndStoreAsync(SetOperation operation, RedisKey destination, RedisKey[] keys, CommandFlags flags = 0) => Task.FromResult(0L);
    public bool SetContains(RedisKey key, RedisValue value, CommandFlags flags = 0) => false;
    public bool[] SetContains(RedisKey key, RedisValue[] values, CommandFlags flags = 0) => [];
    public Task<bool> SetContainsAsync(RedisKey key, RedisValue value, CommandFlags flags = 0) => Task.FromResult(false);
    public Task<bool[]> SetContainsAsync(RedisKey key, RedisValue[] values, CommandFlags flags = 0) => Task.FromResult<bool[]>([]);
    public long SetLength(RedisKey key, CommandFlags flags = 0) => 0;
    public Task<long> SetLengthAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult(0L);
    public RedisValue[] SetMembers(RedisKey key, CommandFlags flags = 0) => [];
    public Task<RedisValue[]> SetMembersAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult<RedisValue[]>([]);
    public bool SetMove(RedisKey source, RedisKey destination, RedisValue value, CommandFlags flags = 0) => false;
    public Task<bool> SetMoveAsync(RedisKey source, RedisKey destination, RedisValue value, CommandFlags flags = 0) => Task.FromResult(false);
    public RedisValue SetPop(RedisKey key, CommandFlags flags = 0) => default;
    public RedisValue[] SetPop(RedisKey key, long count, CommandFlags flags = 0) => [];
    public Task<RedisValue> SetPopAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult(RedisValue.Null);
    public Task<RedisValue[]> SetPopAsync(RedisKey key, long count, CommandFlags flags = 0) => Task.FromResult<RedisValue[]>([]);
    public RedisValue SetRandomMember(RedisKey key, CommandFlags flags = 0) => default;
    public Task<RedisValue> SetRandomMemberAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult(RedisValue.Null);
    public RedisValue[] SetRandomMembers(RedisKey key, long count, CommandFlags flags = 0) => [];
    public Task<RedisValue[]> SetRandomMembersAsync(RedisKey key, long count, CommandFlags flags = 0) => Task.FromResult<RedisValue[]>([]);
    public bool SetRemove(RedisKey key, RedisValue value, CommandFlags flags = 0) => false;
    public long SetRemove(RedisKey key, RedisValue[] values, CommandFlags flags = 0) => 0;
    public Task<bool> SetRemoveAsync(RedisKey key, RedisValue value, CommandFlags flags = 0) => Task.FromResult(false);
    public Task<long> SetRemoveAsync(RedisKey key, RedisValue[] values, CommandFlags flags = 0) => Task.FromResult(0L);
    public IEnumerable<RedisValue> SetScan(RedisKey key, RedisValue pattern = default, int pageSize = 250, long cursor = 0, int pageOffset = 0, CommandFlags flags = 0) => [];
    public IAsyncEnumerable<RedisValue> SetScanAsync(RedisKey key, RedisValue pattern = default, int pageSize = 250, long cursor = 0, int pageOffset = 0, CommandFlags flags = 0) => AsyncEnumerable.Empty<RedisValue>();
    public RedisValue[] Sort(RedisKey key, long skip = 0, long take = -1, Order order = 0, SortType sortType = 0, RedisValue by = default, RedisValue[]? get = null, CommandFlags flags = 0) => [];
    public long SortAndStore(RedisKey destination, RedisKey key, long skip = 0, long take = -1, Order order = 0, SortType sortType = 0, RedisValue by = default, RedisValue[]? get = null, CommandFlags flags = 0) => 0;
    public Task<RedisValue[]> SortAsync(RedisKey key, long skip = 0, long take = -1, Order order = 0, SortType sortType = 0, RedisValue by = default, RedisValue[]? get = null, CommandFlags flags = 0) => Task.FromResult<RedisValue[]>([]);
    public Task<long> SortAndStoreAsync(RedisKey destination, RedisKey key, long skip = 0, long take = -1, Order order = 0, SortType sortType = 0, RedisValue by = default, RedisValue[]? get = null, CommandFlags flags = 0) => Task.FromResult(0L);
    public bool SortedSetAdd(RedisKey key, RedisValue member, double score, SortedSetWhen when = 0, CommandFlags flags = 0) => false;
    public bool SortedSetAdd(RedisKey key, RedisValue member, double score, When when = 0, CommandFlags flags = 0) => false;
    public long SortedSetAdd(RedisKey key, SortedSetEntry[] values, SortedSetWhen when = 0, CommandFlags flags = 0) => 0;
    public long SortedSetAdd(RedisKey key, SortedSetEntry[] values, When when = 0, CommandFlags flags = 0) => 0;
    public Task<bool> SortedSetAddAsync(RedisKey key, RedisValue member, double score, SortedSetWhen when = 0, CommandFlags flags = 0) => Task.FromResult(false);
    public Task<bool> SortedSetAddAsync(RedisKey key, RedisValue member, double score, When when = 0, CommandFlags flags = 0) => Task.FromResult(false);
    public Task<long> SortedSetAddAsync(RedisKey key, SortedSetEntry[] values, SortedSetWhen when = 0, CommandFlags flags = 0) => Task.FromResult(0L);
    public Task<long> SortedSetAddAsync(RedisKey key, SortedSetEntry[] values, When when = 0, CommandFlags flags = 0) => Task.FromResult(0L);
    public RedisValue[] SortedSetCombine(SetOperation operation, RedisKey[] keys, double[]? weights = null, Aggregate aggregate = 0, CommandFlags flags = 0) => [];
    public SortedSetEntry[] SortedSetCombineWithScores(SetOperation operation, RedisKey[] keys, double[]? weights = null, Aggregate aggregate = 0, CommandFlags flags = 0) => [];
    public Task<RedisValue[]> SortedSetCombineAsync(SetOperation operation, RedisKey[] keys, double[]? weights = null, Aggregate aggregate = 0, CommandFlags flags = 0) => Task.FromResult<RedisValue[]>([]);
    public Task<SortedSetEntry[]> SortedSetCombineWithScoresAsync(SetOperation operation, RedisKey[] keys, double[]? weights = null, Aggregate aggregate = 0, CommandFlags flags = 0) => Task.FromResult<SortedSetEntry[]>([]);
    public long SortedSetCombineAndStore(SetOperation operation, RedisKey destination, RedisKey first, RedisKey second, Aggregate aggregate = 0, CommandFlags flags = 0) => 0;
    public long SortedSetCombineAndStore(SetOperation operation, RedisKey destination, RedisKey[] keys, double[]? weights = null, Aggregate aggregate = 0, CommandFlags flags = 0) => 0;
    public Task<long> SortedSetCombineAndStoreAsync(SetOperation operation, RedisKey destination, RedisKey first, RedisKey second, Aggregate aggregate = 0, CommandFlags flags = 0) => Task.FromResult(0L);
    public Task<long> SortedSetCombineAndStoreAsync(SetOperation operation, RedisKey destination, RedisKey[] keys, double[]? weights = null, Aggregate aggregate = 0, CommandFlags flags = 0) => Task.FromResult(0L);
    public double SortedSetDecrement(RedisKey key, RedisValue member, double value, CommandFlags flags = 0) => 0;
    public Task<double> SortedSetDecrementAsync(RedisKey key, RedisValue member, double value, CommandFlags flags = 0) => Task.FromResult(0.0);
    public double SortedSetIncrement(RedisKey key, RedisValue member, double value, CommandFlags flags = 0) => 0;
    public Task<double> SortedSetIncrementAsync(RedisKey key, RedisValue member, double value, CommandFlags flags = 0) => Task.FromResult(0.0);
    public long SortedSetIntersectionLength(RedisKey[] keys, long limit = 0, CommandFlags flags = 0) => 0;
    public Task<long> SortedSetIntersectionLengthAsync(RedisKey[] keys, long limit = 0, CommandFlags flags = 0) => Task.FromResult(0L);
    public long SortedSetLength(RedisKey key, double min = double.NegativeInfinity, double max = double.PositiveInfinity, Exclude exclude = 0, CommandFlags flags = 0) => 0;
    public Task<long> SortedSetLengthAsync(RedisKey key, double min = double.NegativeInfinity, double max = double.PositiveInfinity, Exclude exclude = 0, CommandFlags flags = 0) => Task.FromResult(0L);
    public long SortedSetLengthByValue(RedisKey key, RedisValue min, RedisValue max, Exclude exclude = 0, CommandFlags flags = 0) => 0;
    public Task<long> SortedSetLengthByValueAsync(RedisKey key, RedisValue min, RedisValue max, Exclude exclude = 0, CommandFlags flags = 0) => Task.FromResult(0L);
    public SortedSetEntry? SortedSetPop(RedisKey key, Order order = 0, CommandFlags flags = 0) => null;
    public SortedSetEntry[] SortedSetPop(RedisKey key, long count, Order order = 0, CommandFlags flags = 0) => [];
    public SortedSetPopResult SortedSetPop(RedisKey[] keys, long count, Order order = 0, CommandFlags flags = 0) => default;
    public Task<SortedSetEntry?> SortedSetPopAsync(RedisKey key, Order order = 0, CommandFlags flags = 0) => Task.FromResult<SortedSetEntry?>(null);
    public Task<SortedSetEntry[]> SortedSetPopAsync(RedisKey key, long count, Order order = 0, CommandFlags flags = 0) => Task.FromResult<SortedSetEntry[]>([]);
    public Task<SortedSetPopResult> SortedSetPopAsync(RedisKey[] keys, long count, Order order = 0, CommandFlags flags = 0) => Task.FromResult(default(SortedSetPopResult));
    public long? SortedSetRank(RedisKey key, RedisValue member, Order order = 0, CommandFlags flags = 0) => null;
    public Task<long?> SortedSetRankAsync(RedisKey key, RedisValue member, Order order = 0, CommandFlags flags = 0) => Task.FromResult<long?>(null);
    public SortedSetEntry? SortedSetRandomMember(RedisKey key, CommandFlags flags = 0) => null;
    public RedisValue[] SortedSetRandomMembers(RedisKey key, long count, CommandFlags flags = 0) => [];
    public SortedSetEntry[] SortedSetRandomMembersWithScores(RedisKey key, long count, CommandFlags flags = 0) => [];
    public Task<RedisValue> SortedSetRandomMemberAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult(RedisValue.Null);
    public Task<RedisValue[]> SortedSetRandomMembersAsync(RedisKey key, long count, CommandFlags flags = 0) => Task.FromResult<RedisValue[]>([]);
    public Task<SortedSetEntry[]> SortedSetRandomMembersWithScoresAsync(RedisKey key, long count, CommandFlags flags = 0) => Task.FromResult<SortedSetEntry[]>([]);
    public RedisValue[] SortedSetRangeByRank(RedisKey key, long start = 0, long stop = -1, Order order = 0, CommandFlags flags = 0) => [];
    public SortedSetEntry[] SortedSetRangeByRankWithScores(RedisKey key, long start = 0, long stop = -1, Order order = 0, CommandFlags flags = 0) => [];
    public RedisValue[] SortedSetRangeByScore(RedisKey key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, Exclude exclude = 0, Order order = 0, long skip = 0, long take = -1, CommandFlags flags = 0) => [];
    public SortedSetEntry[] SortedSetRangeByScoreWithScores(RedisKey key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, Exclude exclude = 0, Order order = 0, long skip = 0, long take = -1, CommandFlags flags = 0) => [];
    public RedisValue[] SortedSetRangeByValue(RedisKey key, RedisValue min = default, RedisValue max = default, Exclude exclude = 0, long skip = 0, long take = -1, CommandFlags flags = 0) => [];
    public RedisValue[] SortedSetRangeByValue(RedisKey key, RedisValue min, RedisValue max, Exclude exclude, Order order, long skip, long take = -1, CommandFlags flags = 0) => [];
    public Task<RedisValue[]> SortedSetRangeByRankAsync(RedisKey key, long start = 0, long stop = -1, Order order = 0, CommandFlags flags = 0) => Task.FromResult<RedisValue[]>([]);
    public Task<SortedSetEntry[]> SortedSetRangeByRankWithScoresAsync(RedisKey key, long start = 0, long stop = -1, Order order = 0, CommandFlags flags = 0) => Task.FromResult<SortedSetEntry[]>([]);
    public Task<RedisValue[]> SortedSetRangeByScoreAsync(RedisKey key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, Exclude exclude = 0, Order order = 0, long skip = 0, long take = -1, CommandFlags flags = 0) => Task.FromResult<RedisValue[]>([]);
    public Task<SortedSetEntry[]> SortedSetRangeByScoreWithScoresAsync(RedisKey key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, Exclude exclude = 0, Order order = 0, long skip = 0, long take = -1, CommandFlags flags = 0) => Task.FromResult<SortedSetEntry[]>([]);
    public Task<RedisValue[]> SortedSetRangeByValueAsync(RedisKey key, RedisValue min = default, RedisValue max = default, Exclude exclude = 0, long skip = 0, long take = -1, CommandFlags flags = 0) => Task.FromResult<RedisValue[]>([]);
    public Task<RedisValue[]> SortedSetRangeByValueAsync(RedisKey key, RedisValue min, RedisValue max, Exclude exclude, Order order, long skip, long take = -1, CommandFlags flags = 0) => Task.FromResult<RedisValue[]>([]);
    public long SortedSetRangeAndStore(RedisKey sourceKey, RedisKey destinationKey, RedisValue start, RedisValue stop, SortedSetOrder sortedSetOrder = 0, Exclude exclude = 0, Order order = 0, long skip = 0, long take = -1, CommandFlags flags = 0) => 0;
    public Task<long> SortedSetRangeAndStoreAsync(RedisKey sourceKey, RedisKey destinationKey, RedisValue start, RedisValue stop, SortedSetOrder sortedSetOrder = 0, Exclude exclude = 0, Order order = 0, long skip = 0, long take = -1, CommandFlags flags = 0) => Task.FromResult(0L);
    public bool SortedSetRemove(RedisKey key, RedisValue member, CommandFlags flags = 0) => false;
    public long SortedSetRemove(RedisKey key, RedisValue[] members, CommandFlags flags = 0) => 0;
    public Task<bool> SortedSetRemoveAsync(RedisKey key, RedisValue member, CommandFlags flags = 0) => Task.FromResult(false);
    public Task<long> SortedSetRemoveAsync(RedisKey key, RedisValue[] members, CommandFlags flags = 0) => Task.FromResult(0L);
    public long SortedSetRemoveRangeByRank(RedisKey key, long start, long stop, CommandFlags flags = 0) => 0;
    public Task<long> SortedSetRemoveRangeByRankAsync(RedisKey key, long start, long stop, CommandFlags flags = 0) => Task.FromResult(0L);
    public long SortedSetRemoveRangeByScore(RedisKey key, double start, double stop, Exclude exclude = 0, CommandFlags flags = 0) => 0;
    public Task<long> SortedSetRemoveRangeByScoreAsync(RedisKey key, double start, double stop, Exclude exclude = 0, CommandFlags flags = 0) => Task.FromResult(0L);
    public long SortedSetRemoveRangeByValue(RedisKey key, RedisValue min, RedisValue max, Exclude exclude = 0, CommandFlags flags = 0) => 0;
    public Task<long> SortedSetRemoveRangeByValueAsync(RedisKey key, RedisValue min, RedisValue max, Exclude exclude = 0, CommandFlags flags = 0) => Task.FromResult(0L);
    public IEnumerable<SortedSetEntry> SortedSetScan(RedisKey key, RedisValue pattern = default, int pageSize = 250, long cursor = 0, int pageOffset = 0, CommandFlags flags = 0) => [];
    public IAsyncEnumerable<SortedSetEntry> SortedSetScanAsync(RedisKey key, RedisValue pattern = default, int pageSize = 250, long cursor = 0, int pageOffset = 0, CommandFlags flags = 0) => AsyncEnumerable.Empty<SortedSetEntry>();
    public double? SortedSetScore(RedisKey key, RedisValue member, CommandFlags flags = 0) => null;
    public double?[] SortedSetScores(RedisKey key, RedisValue[] members, CommandFlags flags = 0) => [];
    public Task<double?> SortedSetScoreAsync(RedisKey key, RedisValue member, CommandFlags flags = 0) => Task.FromResult<double?>(null);
    public Task<double?[]> SortedSetScoresAsync(RedisKey key, RedisValue[] members, CommandFlags flags = 0) => Task.FromResult<double?[]>([]);
    public long SortedSetUnionLength(RedisKey[] keys, Aggregate aggregate = 0, CommandFlags flags = 0) => 0;
    public Task<long> SortedSetUnionLengthAsync(RedisKey[] keys, Aggregate aggregate = 0, CommandFlags flags = 0) => Task.FromResult(0L);
    public RedisValue StringAppend(RedisKey key, RedisValue value, CommandFlags flags = 0) => default;
    public Task<RedisValue> StringAppendAsync(RedisKey key, RedisValue value, CommandFlags flags = 0) => Task.FromResult(RedisValue.Null);
    public long StringBitCount(RedisKey key, long start = 0, long end = -1, CommandFlags flags = 0) => 0;
    public long StringBitCount(RedisKey key, long start, long end, StringIndexType indexType, CommandFlags flags = 0) => 0;
    public Task<long> StringBitCountAsync(RedisKey key, long start = 0, long end = -1, CommandFlags flags = 0) => Task.FromResult(0L);
    public Task<long> StringBitCountAsync(RedisKey key, long start, long end, StringIndexType indexType, CommandFlags flags = 0) => Task.FromResult(0L);
    public long StringBitOperation(Bitwise operation, RedisKey destination, RedisKey first, RedisKey second = default, CommandFlags flags = 0) => 0;
    public long StringBitOperation(Bitwise operation, RedisKey destination, RedisKey[] keys, CommandFlags flags = 0) => 0;
    public Task<long> StringBitOperationAsync(Bitwise operation, RedisKey destination, RedisKey first, RedisKey second = default, CommandFlags flags = 0) => Task.FromResult(0L);
    public Task<long> StringBitOperationAsync(Bitwise operation, RedisKey destination, RedisKey[] keys, CommandFlags flags = 0) => Task.FromResult(0L);
    public long StringBitPosition(RedisKey key, bool bit, long start = 0, long end = -1, CommandFlags flags = 0) => 0;
    public long StringBitPosition(RedisKey key, bool bit, long start, long end, StringIndexType indexType, CommandFlags flags = 0) => 0;
    public Task<long> StringBitPositionAsync(RedisKey key, bool bit, long start = 0, long end = -1, CommandFlags flags = 0) => Task.FromResult(0L);
    public Task<long> StringBitPositionAsync(RedisKey key, bool bit, long start, long end, StringIndexType indexType, CommandFlags flags = 0) => Task.FromResult(0L);
    public long StringDecrement(RedisKey key, long value = 1, CommandFlags flags = 0) => 0;
    public double StringDecrement(RedisKey key, double value, CommandFlags flags = 0) => 0;
    public Task<long> StringDecrementAsync(RedisKey key, long value = 1, CommandFlags flags = 0) => Task.FromResult(0L);
    public Task<double> StringDecrementAsync(RedisKey key, double value, CommandFlags flags = 0) => Task.FromResult(0.0);
    public RedisValue StringGet(RedisKey key, CommandFlags flags = 0) => default;
    public RedisValue[] StringGet(RedisKey[] keys, CommandFlags flags = 0) => [];
    public Lease<byte>? StringGetLease(RedisKey key, CommandFlags flags = 0) => null;
    public Task<Lease<byte>?> StringGetLeaseAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult<Lease<byte>?>(null);
    public Task<RedisValue> StringGetAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult(RedisValue.Null);
    public Task<RedisValue[]> StringGetAsync(RedisKey[] keys, CommandFlags flags = 0) => Task.FromResult<RedisValue[]>([]);
    public bool StringGetBit(RedisKey key, long offset, CommandFlags flags = 0) => false;
    public Task<bool> StringGetBitAsync(RedisKey key, long offset, CommandFlags flags = 0) => Task.FromResult(false);
    public RedisValue StringGetRange(RedisKey key, long start, long end, CommandFlags flags = 0) => default;
    public Task<RedisValue> StringGetRangeAsync(RedisKey key, long start, long end, CommandFlags flags = 0) => Task.FromResult(RedisValue.Null);
    public RedisValue StringGetSet(RedisKey key, RedisValue value, CommandFlags flags = 0) => default;
    public Task<RedisValue> StringGetSetAsync(RedisKey key, RedisValue value, CommandFlags flags = 0) => Task.FromResult(RedisValue.Null);
    public RedisValue StringGetSetExpiry(RedisKey key, TimeSpan? expiry, CommandFlags flags = 0) => default;
    public RedisValue StringGetSetExpiry(RedisKey key, DateTime expiry, CommandFlags flags = 0) => default;
    public Task<RedisValue> StringGetSetExpiryAsync(RedisKey key, TimeSpan? expiry, CommandFlags flags = 0) => Task.FromResult(RedisValue.Null);
    public Task<RedisValue> StringGetSetExpiryAsync(RedisKey key, DateTime expiry, CommandFlags flags = 0) => Task.FromResult(RedisValue.Null);
    public RedisValueWithExpiry StringGetWithExpiry(RedisKey key, CommandFlags flags = 0) => default;
    public Task<RedisValueWithExpiry> StringGetWithExpiryAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult(default(RedisValueWithExpiry));
    public long StringIncrement(RedisKey key, long value = 1, CommandFlags flags = 0) => 0;
    public double StringIncrement(RedisKey key, double value, CommandFlags flags = 0) => 0;
    public Task<long> StringIncrementAsync(RedisKey key, long value = 1, CommandFlags flags = 0) => Task.FromResult(0L);
    public Task<double> StringIncrementAsync(RedisKey key, double value, CommandFlags flags = 0) => Task.FromResult(0.0);
    public long StringLength(RedisKey key, CommandFlags flags = 0) => 0;
    public Task<long> StringLengthAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult(0L);
    public bool StringSet(RedisKey key, RedisValue value, TimeSpan? expiry = null, bool keepTtl = false, When when = 0, CommandFlags flags = 0) => false;
    public bool StringSet(RedisKey key, RedisValue value, TimeSpan? expiry, When when, CommandFlags flags = 0) => false;
    public bool StringSet(KeyValuePair<RedisKey, RedisValue>[] values, When when = 0, CommandFlags flags = 0) => false;
    public Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null, bool keepTtl = false, When when = 0, CommandFlags flags = 0) => Task.FromResult(false);
    public Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry, When when, CommandFlags flags = 0) => Task.FromResult(false);
    public Task<bool> StringSetAsync(KeyValuePair<RedisKey, RedisValue>[] values, When when = 0, CommandFlags flags = 0) => Task.FromResult(false);
    public bool StringSetAndGet(RedisKey key, RedisValue value, TimeSpan? expiry, When when = 0, CommandFlags flags = 0) => false;
    public RedisValue StringSetAndGet(RedisKey key, RedisValue value, TimeSpan? expiry, bool keepTtl, When when = 0, CommandFlags flags = 0) => default;
    public Task<bool> StringSetAndGetAsync(RedisKey key, RedisValue value, TimeSpan? expiry, When when = 0, CommandFlags flags = 0) => Task.FromResult(false);
    public Task<RedisValue> StringSetAndGetAsync(RedisKey key, RedisValue value, TimeSpan? expiry, bool keepTtl, When when = 0, CommandFlags flags = 0) => Task.FromResult(RedisValue.Null);
    public bool StringSetBit(RedisKey key, long offset, bool bit, CommandFlags flags = 0) => false;
    public Task<bool> StringSetBitAsync(RedisKey key, long offset, bool bit, CommandFlags flags = 0) => Task.FromResult(false);
    public RedisValue StringSetRange(RedisKey key, long offset, RedisValue value, CommandFlags flags = 0) => default;
    public Task<RedisValue> StringSetRangeAsync(RedisKey key, long offset, RedisValue value, CommandFlags flags = 0) => Task.FromResult(RedisValue.Null);
    public bool TryWait(Task task) => task.IsCompleted;
    public RedisValue StreamAdd(RedisKey key, RedisValue streamField, RedisValue streamValue, RedisValue? messageId = null, int? maxLength = null, bool useApproximateMaxLength = false, CommandFlags flags = 0) => default;
    public RedisValue StreamAdd(RedisKey key, NameValueEntry[] streamPairs, RedisValue? messageId = null, int? maxLength = null, bool useApproximateMaxLength = false, CommandFlags flags = 0) => default;
    public Task<RedisValue> StreamAddAsync(RedisKey key, RedisValue streamField, RedisValue streamValue, RedisValue? messageId = null, int? maxLength = null, bool useApproximateMaxLength = false, CommandFlags flags = 0) => Task.FromResult(RedisValue.Null);
    public Task<RedisValue> StreamAddAsync(RedisKey key, NameValueEntry[] streamPairs, RedisValue? messageId = null, int? maxLength = null, bool useApproximateMaxLength = false, CommandFlags flags = 0) => Task.FromResult(RedisValue.Null);
    public StreamAutoClaimResult StreamAutoClaim(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer, long minIdleTimeInMs, RedisValue startAtId, int? count = null, CommandFlags flags = 0) => default;
    public StreamAutoClaimIdsOnlyResult StreamAutoClaimIdsOnly(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer, long minIdleTimeInMs, RedisValue startAtId, int? count = null, CommandFlags flags = 0) => default;
    public Task<StreamAutoClaimResult> StreamAutoClaimAsync(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer, long minIdleTimeInMs, RedisValue startAtId, int? count = null, CommandFlags flags = 0) => Task.FromResult(default(StreamAutoClaimResult));
    public Task<StreamAutoClaimIdsOnlyResult> StreamAutoClaimIdsOnlyAsync(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer, long minIdleTimeInMs, RedisValue startAtId, int? count = null, CommandFlags flags = 0) => Task.FromResult(default(StreamAutoClaimIdsOnlyResult));
    public StreamEntry[] StreamClaim(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer, long minIdleTimeInMs, RedisValue[] messageIds, CommandFlags flags = 0) => [];
    public RedisValue[] StreamClaimIdsOnly(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer, long minIdleTimeInMs, RedisValue[] messageIds, CommandFlags flags = 0) => [];
    public Task<StreamEntry[]> StreamClaimAsync(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer, long minIdleTimeInMs, RedisValue[] messageIds, CommandFlags flags = 0) => Task.FromResult<StreamEntry[]>([]);
    public Task<RedisValue[]> StreamClaimIdsOnlyAsync(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer, long minIdleTimeInMs, RedisValue[] messageIds, CommandFlags flags = 0) => Task.FromResult<RedisValue[]>([]);
    public bool StreamConsumerGroupSetPosition(RedisKey key, RedisValue groupName, RedisValue position, CommandFlags flags = 0) => false;
    public Task<bool> StreamConsumerGroupSetPositionAsync(RedisKey key, RedisValue groupName, RedisValue position, CommandFlags flags = 0) => Task.FromResult(false);
    public StreamConsumerInfo[] StreamConsumerInfo(RedisKey key, RedisValue groupName, CommandFlags flags = 0) => [];
    public Task<StreamConsumerInfo[]> StreamConsumerInfoAsync(RedisKey key, RedisValue groupName, CommandFlags flags = 0) => Task.FromResult<StreamConsumerInfo[]>([]);
    public bool StreamCreateConsumerGroup(RedisKey key, RedisValue groupName, RedisValue? position = null, bool createStream = true, CommandFlags flags = 0) => false;
    public Task<bool> StreamCreateConsumerGroupAsync(RedisKey key, RedisValue groupName, RedisValue? position = null, bool createStream = true, CommandFlags flags = 0) => Task.FromResult(false);
    public long StreamDelete(RedisKey key, RedisValue[] messageIds, CommandFlags flags = 0) => 0;
    public Task<long> StreamDeleteAsync(RedisKey key, RedisValue[] messageIds, CommandFlags flags = 0) => Task.FromResult(0L);
    public long StreamDeleteConsumer(RedisKey key, RedisValue groupName, RedisValue consumerName, CommandFlags flags = 0) => 0;
    public Task<long> StreamDeleteConsumerAsync(RedisKey key, RedisValue groupName, RedisValue consumerName, CommandFlags flags = 0) => Task.FromResult(0L);
    public bool StreamDeleteConsumerGroup(RedisKey key, RedisValue groupName, CommandFlags flags = 0) => false;
    public Task<bool> StreamDeleteConsumerGroupAsync(RedisKey key, RedisValue groupName, CommandFlags flags = 0) => Task.FromResult(false);
    public StreamGroupInfo[] StreamGroupInfo(RedisKey key, CommandFlags flags = 0) => [];
    public Task<StreamGroupInfo[]> StreamGroupInfoAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult<StreamGroupInfo[]>([]);
    public StreamInfo StreamInfo(RedisKey key, CommandFlags flags = 0) => default;
    public Task<StreamInfo> StreamInfoAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult(default(StreamInfo));
    public long StreamLength(RedisKey key, CommandFlags flags = 0) => 0;
    public Task<long> StreamLengthAsync(RedisKey key, CommandFlags flags = 0) => Task.FromResult(0L);
    public StreamPendingInfo StreamPending(RedisKey key, RedisValue groupName, CommandFlags flags = 0) => default;
    public Task<StreamPendingInfo> StreamPendingAsync(RedisKey key, RedisValue groupName, CommandFlags flags = 0) => Task.FromResult(default(StreamPendingInfo));
    public StreamPendingMessageInfo[] StreamPendingMessages(RedisKey key, RedisValue groupName, int count, RedisValue consumerName, RedisValue? minId = null, RedisValue? maxId = null, CommandFlags flags = 0) => [];
    public Task<StreamPendingMessageInfo[]> StreamPendingMessagesAsync(RedisKey key, RedisValue groupName, int count, RedisValue consumerName, RedisValue? minId = null, RedisValue? maxId = null, CommandFlags flags = 0) => Task.FromResult<StreamPendingMessageInfo[]>([]);
    public StreamEntry[] StreamRange(RedisKey key, RedisValue? minId = null, RedisValue? maxId = null, int? count = null, Order messageOrder = 0, CommandFlags flags = 0) => [];
    public Task<StreamEntry[]> StreamRangeAsync(RedisKey key, RedisValue? minId = null, RedisValue? maxId = null, int? count = null, Order messageOrder = 0, CommandFlags flags = 0) => Task.FromResult<StreamEntry[]>([]);
    public StreamEntry[] StreamRead(RedisKey key, RedisValue position, int? count = null, CommandFlags flags = 0) => [];
    public RedisStream[] StreamRead(StreamPosition[] streamPositions, int? countPerStream = null, CommandFlags flags = 0) => [];
    public Task<StreamEntry[]> StreamReadAsync(RedisKey key, RedisValue position, int? count = null, CommandFlags flags = 0) => Task.FromResult<StreamEntry[]>([]);
    public Task<RedisStream[]> StreamReadAsync(StreamPosition[] streamPositions, int? countPerStream = null, CommandFlags flags = 0) => Task.FromResult<RedisStream[]>([]);
    public StreamEntry[] StreamReadGroup(RedisKey key, RedisValue groupName, RedisValue consumerName, RedisValue? position = null, int? count = null, bool noAck = false, CommandFlags flags = 0) => [];
    public RedisStream[] StreamReadGroup(StreamPosition[] streamPositions, RedisValue groupName, RedisValue consumerName, int? countPerStream = null, bool noAck = false, CommandFlags flags = 0) => [];
    public Task<StreamEntry[]> StreamReadGroupAsync(RedisKey key, RedisValue groupName, RedisValue consumerName, RedisValue? position = null, int? count = null, bool noAck = false, CommandFlags flags = 0) => Task.FromResult<StreamEntry[]>([]);
    public Task<RedisStream[]> StreamReadGroupAsync(StreamPosition[] streamPositions, RedisValue groupName, RedisValue consumerName, int? countPerStream = null, bool noAck = false, CommandFlags flags = 0) => Task.FromResult<RedisStream[]>([]);
    public long StreamTrim(RedisKey key, int maxLength, bool useApproximateMaxLength = false, CommandFlags flags = 0) => 0;
    public Task<long> StreamTrimAsync(RedisKey key, int maxLength, bool useApproximateMaxLength = false, CommandFlags flags = 0) => Task.FromResult(0L);
}
