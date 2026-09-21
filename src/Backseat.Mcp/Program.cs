using Backseat.Backends.Cua;
using Backseat.Core;
using Backseat.Mcp;

var runsRoot = (string?)null;
var deliveryPolicy = DeliveryPolicy.BackgroundOnly;

for (var index = 0; index < args.Length; index++)
{
    switch (args[index])
    {
        case "--runs":
            runsRoot = index + 1 < args.Length ? args[++index] : throw new ArgumentException("--runs requires a directory.");
            break;
        case "--allow-foreground":
            deliveryPolicy = DeliveryPolicy.AllowForeground;
            break;
        default:
            Console.Error.WriteLine($"Unknown argument '{args[index]}'. Supported: --runs DIR, --allow-foreground.");
            return 2;
    }
}

var backend = new CuaCliBackend(new CuaProcessRunner(), allowForegroundRetry: deliveryPolicy == DeliveryPolicy.AllowForeground);

await using var server = new McpServer(backend, Console.In, Console.Out, runsRoot, deliveryPolicy);
return await server.RunAsync();
