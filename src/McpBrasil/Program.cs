using McpBrasil.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// IMPORTANTE: no transporte stdio, o stdout é o canal do protocolo MCP.
// Qualquer Console.WriteLine ou log no stdout corrompe a comunicação.
// Por isso, todos os logs vão para o stderr.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

// Cliente HTTP tipado para a BrasilAPI (injetado nas ferramentas)
builder.Services.AddHttpClient<BrasilApiClient>(client =>
{
    client.BaseAddress = new Uri("https://brasilapi.com.br/api/");
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("mcp-brasil/0.1");
});

// Servidor MCP: transporte stdio + descobre automaticamente as classes
// marcadas com [McpServerToolType] neste assembly
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
