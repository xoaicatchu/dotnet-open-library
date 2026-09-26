using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace RealtimeTaskBoard.LoadTester;

public class LoadTestConfig
{
    public int TargetConnections { get; set; } = 100;
    public string Scenario { get; set; } = "idle"; // "idle", "interactive", "broadcast"
    public string BaseUrl { get; set; } = "http://localhost:5155";
    public int[] Ports { get; set; } = [5155, 5156, 5157, 5158];
    public int RampRate { get; set; } = 500; // Connections per second
    public int HoldSeconds { get; set; } = 10;
    public int SendIntervalMs { get; set; } = 2000;
    public int PayloadSizeBytes { get; set; } = 256;
    public string? AuthToken { get; set; }
    public string BoardId { get; set; } = "9a984d76-55c9-45aa-aeed-6da67fda657b";
    public bool SkipNegotiate { get; set; } = true;
    public string? ReportJsonPath { get; set; }
}

public class LoadTestResult
{
    public string Scenario { get; set; } = "";
    public int TargetConnections { get; set; }
    public int ConnectedCount { get; set; }
    public int FailedCount { get; set; }
    public int ReconnectCount { get; set; }
    public double MessagesPerSecond { get; set; }
    public long TotalMessagesReceived { get; set; }
    public long TotalMessagesSent { get; set; }
    public double LatencyP50Ms { get; set; }
    public double LatencyP95Ms { get; set; }
    public double LatencyP99Ms { get; set; }
    public double ServerRamMb { get; set; }
    public double ServerCpuPercent { get; set; }
    public double ClientRamMb { get; set; }
    public double ClientCpuPercent { get; set; }
    public double RampDurationSec { get; set; }
    public double HoldDurationSec { get; set; }
    public Dictionary<string, int> ErrorBreakdown { get; set; } = new();
    public string BottleneckAssessment { get; set; } = "None";
}

class Program
{
    static async Task<int> Main(string[] args)
    {
        var config = ParseConfiguration(args);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("===============================================================================");
        Console.WriteLine($"   SignalR Load Tester | Scenario: {config.Scenario.ToUpper()} | Target: {config.TargetConnections:N0} conns");
        Console.WriteLine($"   Base URL: {config.BaseUrl} | Ports: [{string.Join(", ", config.Ports)}]");
        Console.WriteLine($"   Ramp Rate: {config.RampRate} conns/sec | Hold: {config.HoldSeconds}s | Board ID: {config.BoardId}");
        Console.WriteLine($"   Skip Negotiate: {config.SkipNegotiate} | Payload Size: {config.PayloadSizeBytes} bytes");
        Console.WriteLine("===============================================================================");
        Console.ResetColor();

        // 1. Verify target server is reachable
        using var checkClient = new HttpClient();
        try
        {
            var ping = await checkClient.GetAsync($"{config.BaseUrl}/api/boards");
            if (!ping.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] Target server at {config.BaseUrl} returned {ping.StatusCode}. Aborting.");
                Console.ResetColor();
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] Cannot connect to {config.BaseUrl}: {ex.Message}. Aborting.");
            Console.ResetColor();
            return 1;
        }

        var result = await RunLoadTestAsync(config);

        // Print final summary
        PrintResultSummary(result);

        if (!string.IsNullOrEmpty(config.ReportJsonPath))
        {
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(config.ReportJsonPath, json);
            Console.WriteLine($"[Export] Results saved to {config.ReportJsonPath}");
        }

        return result.FailedCount == 0 ? 0 : 2;
    }

    static LoadTestConfig ParseConfiguration(string[] args)
    {
        var cfg = new LoadTestConfig();

        // Environment variable defaults
        if (int.TryParse(Environment.GetEnvironmentVariable("LOADTEST_CONNECTIONS"), out var envConns)) cfg.TargetConnections = envConns;
        if (Environment.GetEnvironmentVariable("LOADTEST_SCENARIO") is { } envScen) cfg.Scenario = envScen;
        if (Environment.GetEnvironmentVariable("LOADTEST_URL") is { } envUrl) cfg.BaseUrl = envUrl;
        if (int.TryParse(Environment.GetEnvironmentVariable("LOADTEST_RAMP_RATE"), out var envRamp)) cfg.RampRate = envRamp;
        if (int.TryParse(Environment.GetEnvironmentVariable("LOADTEST_HOLD_SECONDS"), out var envHold)) cfg.HoldSeconds = envHold;
        if (int.TryParse(Environment.GetEnvironmentVariable("LOADTEST_SEND_INTERVAL_MS"), out var envInterval)) cfg.SendIntervalMs = envInterval;
        if (int.TryParse(Environment.GetEnvironmentVariable("LOADTEST_PAYLOAD_SIZE"), out var envPayload)) cfg.PayloadSizeBytes = envPayload;
        if (Environment.GetEnvironmentVariable("LOADTEST_TOKEN") is { } envTok) cfg.AuthToken = envTok;
        if (Environment.GetEnvironmentVariable("LOADTEST_BOARD_ID") is { } envBid) cfg.BoardId = envBid;
        if (bool.TryParse(Environment.GetEnvironmentVariable("LOADTEST_SKIP_NEGOTIATE"), out var envSkipNeg)) cfg.SkipNegotiate = envSkipNeg;
        if (Environment.GetEnvironmentVariable("LOADTEST_PORTS") is { } envPorts)
        {
            cfg.Ports = envPorts.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => int.Parse(p.Trim())).ToArray();
        }

        // Positional or flagged CLI args override env vars
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Equals("--connections", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) cfg.TargetConnections = int.Parse(args[++i]);
            else if (arg.Equals("--scenario", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) cfg.Scenario = args[++i];
            else if (arg.Equals("--url", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) cfg.BaseUrl = args[++i];
            else if (arg.Equals("--ramp-rate", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) cfg.RampRate = int.Parse(args[++i]);
            else if (arg.Equals("--hold", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) cfg.HoldSeconds = int.Parse(args[++i]);
            else if (arg.Equals("--interval", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) cfg.SendIntervalMs = int.Parse(args[++i]);
            else if (arg.Equals("--payload-size", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) cfg.PayloadSizeBytes = int.Parse(args[++i]);
            else if (arg.Equals("--token", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) cfg.AuthToken = args[++i];
            else if (arg.Equals("--board-id", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) cfg.BoardId = args[++i];
            else if (arg.Equals("--skip-negotiate", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) cfg.SkipNegotiate = bool.Parse(args[++i]);
            else if (arg.Equals("--report-json", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) cfg.ReportJsonPath = args[++i];
            else if (i == 0 && int.TryParse(arg, out var c1)) cfg.TargetConnections = c1;
            else if (i == 1) cfg.Scenario = arg;
            else if (i == 2 && int.TryParse(arg, out var c3)) cfg.HoldSeconds = c3;
        }

        return cfg;
    }

    static async Task<LoadTestResult> RunLoadTestAsync(LoadTestConfig config)
    {
        var connections = new ConcurrentBag<HubConnection>();
        var latenciesMs = new ConcurrentBag<double>();
        var errorSamples = new ConcurrentDictionary<string, int>();

        int connectedCount = 0;
        int failedCount = 0;
        int reconnectCount = 0;
        long messagesReceived = 0;
        long messagesSent = 0;
        long portIndexCounter = 0;

        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var serverProcess = Process.GetProcessesByName("RealtimeTaskBoard.Api").FirstOrDefault();
        var clientProcess = Process.GetCurrentProcess();

        // Batch sizing based on ramp rate
        int batchSize = Math.Clamp(config.RampRate / 10, 10, 500);
        int batchDelayMs = Math.Max(10, 1000 / (config.RampRate / batchSize));

        var rampStopwatch = Stopwatch.StartNew();

        Console.WriteLine($"[Phase 1] Ramping up {config.TargetConnections:N0} connections (Batch {batchSize} every {batchDelayMs}ms)...");

        for (int i = 0; i < config.TargetConnections; i += batchSize)
        {
            int currentBatch = Math.Min(batchSize, config.TargetConnections - i);
            var tasks = new List<Task>(currentBatch);

            for (int b = 0; b < currentBatch; b++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var port = config.Ports[Interlocked.Increment(ref portIndexCounter) % config.Ports.Length];
                        var hubUri = new UriBuilder(config.BaseUrl) { Port = port, Path = "/hubs/task" }.Uri;

                        var builder = new HubConnectionBuilder()
                            .WithUrl(hubUri, options =>
                            {
                                if (config.SkipNegotiate)
                                {
                                    options.Transports = HttpTransportType.WebSockets;
                                    options.SkipNegotiation = true;
                                }
                                if (!string.IsNullOrEmpty(config.AuthToken))
                                {
                                    options.AccessTokenProvider = () => Task.FromResult<string?>(config.AuthToken);
                                }
                            })
                            .WithAutomaticReconnect();

                        var conn = builder.Build();

                        conn.Reconnecting += _ =>
                        {
                            Interlocked.Increment(ref reconnectCount);
                            return Task.CompletedTask;
                        };

                        conn.On<object>("TaskCreated", raw =>
                        {
                            Interlocked.Increment(ref messagesReceived);
                            // If timestamp is present in raw, measure latency
                            if (raw is JsonElement elem && elem.TryGetProperty("description", out var descProp))
                            {
                                var desc = descProp.GetString();
                                if (desc != null && desc.StartsWith("TS:") && long.TryParse(desc[3..], out var sentTicks))
                                {
                                    var elapsed = (Stopwatch.GetTimestamp() - sentTicks) * 1000.0 / Stopwatch.Frequency;
                                    latenciesMs.Add(elapsed);
                                }
                            }
                        });

                        await conn.StartAsync(cancellationToken);
                        await conn.InvokeAsync("JoinBoard", config.BoardId, cancellationToken);

                        connections.Add(conn);
                        Interlocked.Increment(ref connectedCount);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref failedCount);
                        var errName = ex.GetType().Name + ": " + ex.Message.Split('\n')[0].Trim();
                        errorSamples.AddOrUpdate(errName, 1, (_, c) => c + 1);
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);

            if (batchDelayMs > 0)
            {
                await Task.Delay(batchDelayMs, cancellationToken);
            }

            if ((i + currentBatch) % Math.Max(500, config.TargetConnections / 5) == 0 || (i + currentBatch) == config.TargetConnections)
            {
                serverProcess?.Refresh();
                clientProcess.Refresh();
                long serverMb = serverProcess != null ? serverProcess.WorkingSet64 / (1024 * 1024) : 0;
                long clientMb = clientProcess.WorkingSet64 / (1024 * 1024);
                Console.WriteLine($"   [Ramp] Connected: {connectedCount,6:N0}/{config.TargetConnections:N0} | Failed: {failedCount} | API RAM: {serverMb}MB | Tester RAM: {clientMb}MB | Time: {rampStopwatch.Elapsed.TotalSeconds:F1}s");
            }

            // Early abort if catastrophic error rate (> 25% failure)
            if (failedCount > 50 && failedCount > (connectedCount + failedCount) * 0.25)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ABORT] Error rate exceeded 25%. Ceasing ramp-up.");
                Console.ResetColor();
                break;
            }
        }

        rampStopwatch.Stop();
        Console.WriteLine($"[Phase 1 Complete] Connected: {connectedCount:N0} | Failed: {failedCount:N0} | Duration: {rampStopwatch.Elapsed.TotalSeconds:F1}s");

        // Phase 2: Active Scenario Execution
        Console.WriteLine();
        Console.WriteLine($"[Phase 2] Executing scenario '{config.Scenario.ToUpper()}' for {config.HoldSeconds}s...");
        var holdStopwatch = Stopwatch.StartNew();

        Task? backgroundActivityTask = null;

        if (config.Scenario.Equals("broadcast", StringComparison.OrdinalIgnoreCase))
        {
            backgroundActivityTask = Task.Run(async () =>
            {
                using var http = new HttpClient { BaseAddress = new Uri(config.BaseUrl) };
                // Get column to post to
                var colResp = await http.GetStringAsync($"/api/boards/{config.BoardId}/columns");
                using var doc = JsonDocument.Parse(colResp);
                var colId = doc.RootElement[0].GetProperty("id").GetString();

                // Send broadcasts every 250ms
                while (!cancellationToken.IsCancellationRequested && holdStopwatch.Elapsed.TotalSeconds < config.HoldSeconds)
                {
                    try
                    {
                        var timestamp = Stopwatch.GetTimestamp();
                        var payload = new
                        {
                            title = $"Broadcast {Interlocked.Increment(ref messagesSent)}",
                            description = $"TS:{timestamp}",
                            priority = "Low"
                        };
                        var resp = await http.PostAsJsonAsync($"/api/columns/{colId}/tasks", payload, cancellationToken);
                        await Task.Delay(250, cancellationToken);
                    }
                    catch { }
                }
            }, cancellationToken);
        }
        else if (config.Scenario.Equals("interactive", StringComparison.OrdinalIgnoreCase))
        {
            backgroundActivityTask = Task.Run(async () =>
            {
                // Select 5% of active connections to periodically invoke JoinBoard/LeaveBoard and send tasks
                var activeList = connections.Take(Math.Max(1, connectedCount / 20)).ToList();
                while (!cancellationToken.IsCancellationRequested && holdStopwatch.Elapsed.TotalSeconds < config.HoldSeconds)
                {
                    foreach (var conn in activeList)
                    {
                        try
                        {
                            await conn.InvokeAsync("JoinBoard", config.BoardId, cancellationToken);
                            Interlocked.Increment(ref messagesSent);
                        }
                        catch { }
                    }
                    await Task.Delay(config.SendIntervalMs, cancellationToken);
                }
            }, cancellationToken);
        }

        // Monitoring loop during hold
        double maxServerRam = 0;
        double maxClientRam = 0;
        var startServerCpu = serverProcess?.TotalProcessorTime ?? TimeSpan.Zero;
        var startClientCpu = clientProcess.TotalProcessorTime;
        var startWallTime = Stopwatch.GetTimestamp();

        while (holdStopwatch.Elapsed.TotalSeconds < config.HoldSeconds)
        {
            await Task.Delay(2000);
            serverProcess?.Refresh();
            clientProcess.Refresh();

            double serverMb = serverProcess != null ? serverProcess.WorkingSet64 / (1024.0 * 1024.0) : 0;
            double clientMb = clientProcess.WorkingSet64 / (1024.0 * 1024.0);

            maxServerRam = Math.Max(maxServerRam, serverMb);
            maxClientRam = Math.Max(maxClientRam, clientMb);

            Console.WriteLine($"   [Hold {holdStopwatch.Elapsed.TotalSeconds:F0}s/{config.HoldSeconds}s] Conns: {connectedCount:N0} | Msgs Rcvd: {messagesReceived:N0} | API RAM: {serverMb:F0}MB | Tester RAM: {clientMb:F0}MB");
        }

        cts.Cancel();
        if (backgroundActivityTask != null)
        {
            try { await backgroundActivityTask; } catch { }
        }

        holdStopwatch.Stop();

        // Calculate CPU usage
        var totalWallSeconds = (Stopwatch.GetTimestamp() - startWallTime) / (double)Stopwatch.Frequency;
        var endServerCpu = serverProcess?.TotalProcessorTime ?? TimeSpan.Zero;
        var endClientCpu = clientProcess.TotalProcessorTime;

        double serverCpuPct = totalWallSeconds > 0
            ? Math.Clamp((endServerCpu - startServerCpu).TotalSeconds / (Environment.ProcessorCount * totalWallSeconds) * 100.0, 0, 100)
            : 0;

        double clientCpuPct = totalWallSeconds > 0
            ? Math.Clamp((endClientCpu - startClientCpu).TotalSeconds / (Environment.ProcessorCount * totalWallSeconds) * 100.0, 0, 100)
            : 0;

        // Calculate latency percentiles
        var latList = latenciesMs.ToList();
        latList.Sort();
        double p50 = latList.Count > 0 ? latList[(int)(latList.Count * 0.50)] : 0;
        double p95 = latList.Count > 0 ? latList[(int)(latList.Count * 0.95)] : 0;
        double p99 = latList.Count > 0 ? latList[(int)(latList.Count * 0.99)] : 0;

        double msgPerSec = holdStopwatch.Elapsed.TotalSeconds > 0
            ? messagesReceived / holdStopwatch.Elapsed.TotalSeconds
            : 0;

        // Teardown connections
        Console.WriteLine("[Phase 3] Teardown: closing connections...");
        var closeTasks = connections.Select(c => Task.Run(async () =>
        {
            try { await c.DisposeAsync(); } catch { }
        }));
        await Task.WhenAll(closeTasks);

        // Determine bottleneck
        string bottleneck = "None (System Healthy)";
        if (failedCount > 0)
        {
            if (errorSamples.Keys.Any(k => k.Contains("lacked sufficient buffer space") || k.Contains("Only one usage of each socket address") || k.Contains("WSAENOBUFS")))
            {
                bottleneck = "Client OS Ephemeral Port / Winsock Buffer Exhaustion";
            }
            else if (serverCpuPct > 85)
            {
                bottleneck = "Server CPU Saturation";
            }
            else if (clientCpuPct > 85)
            {
                bottleneck = "Client Load Generator CPU Saturation";
            }
            else
            {
                bottleneck = "Network/Socket Rejection";
            }
        }

        return new LoadTestResult
        {
            Scenario = config.Scenario,
            TargetConnections = config.TargetConnections,
            ConnectedCount = connectedCount,
            FailedCount = failedCount,
            ReconnectCount = reconnectCount,
            MessagesPerSecond = Math.Round(msgPerSec, 1),
            TotalMessagesReceived = messagesReceived,
            TotalMessagesSent = messagesSent,
            LatencyP50Ms = Math.Round(p50, 1),
            LatencyP95Ms = Math.Round(p95, 1),
            LatencyP99Ms = Math.Round(p99, 1),
            ServerRamMb = Math.Round(maxServerRam, 1),
            ServerCpuPercent = Math.Round(serverCpuPct, 1),
            ClientRamMb = Math.Round(maxClientRam, 1),
            ClientCpuPercent = Math.Round(clientCpuPct, 1),
            RampDurationSec = Math.Round(rampStopwatch.Elapsed.TotalSeconds, 1),
            HoldDurationSec = Math.Round(holdStopwatch.Elapsed.TotalSeconds, 1),
            ErrorBreakdown = new Dictionary<string, int>(errorSamples),
            BottleneckAssessment = bottleneck
        };
    }

    static void PrintResultSummary(LoadTestResult res)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("===============================================================================");
        Console.WriteLine($"   LOAD TEST REPORT: {res.Scenario.ToUpper()} @ {res.TargetConnections:N0} TARGET CONNECTIONS");
        Console.WriteLine("===============================================================================");
        Console.ResetColor();

        Console.WriteLine($"  Connected:          {res.ConnectedCount:N0} / {res.TargetConnections:N0} ({(res.TargetConnections > 0 ? res.ConnectedCount * 100.0 / res.TargetConnections : 0):F1}%)");
        Console.WriteLine($"  Connection Errors:  {res.FailedCount:N0}");
        Console.WriteLine($"  Reconnections:      {res.ReconnectCount:N0}");
        Console.WriteLine($"  Msg Throughput:     {res.MessagesPerSecond:N1} msgs/sec (Total Rcvd: {res.TotalMessagesReceived:N0}, Sent: {res.TotalMessagesSent:N0})");
        Console.WriteLine($"  Latency p50/p95/p99:{res.LatencyP50Ms:F1}ms / {res.LatencyP95Ms:F1}ms / {res.LatencyP99Ms:F1}ms");
        Console.WriteLine($"  Server Resource:    RAM {res.ServerRamMb:F0} MB | CPU ~{res.ServerCpuPercent:F1}%");
        Console.WriteLine($"  Generator Resource: RAM {res.ClientRamMb:F0} MB | CPU ~{res.ClientCpuPercent:F1}%");
        Console.WriteLine($"  Bottleneck Check:   {res.BottleneckAssessment}");

        if (res.ErrorBreakdown.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Error Details:");
            foreach (var (err, cnt) in res.ErrorBreakdown)
            {
                Console.WriteLine($"    - [{cnt}x] {err}");
            }
            Console.ResetColor();
        }
        Console.WriteLine("===============================================================================");
        Console.WriteLine();
    }
}
