using ChaoticCupid.Server.Contracts;
using ChaoticCupid.Server.Hubs;
using ChaoticCupid.Server.Services;
using ChaoticCupid.Server.State;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

// Shared, thread-safe state used by both the hub and Cupid.
builder.Services.AddSingleton<PersonRegistry>();

// Single CupidService instance exposed both as the hosted background worker
// and as the ICupidService contract.
builder.Services.AddSingleton<CupidService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<CupidService>());
builder.Services.AddSingleton<ICupidService>(sp => sp.GetRequiredService<CupidService>());

var app = builder.Build();

app.MapHub<CupidHub>("/cupid");
app.MapGet("/", () => "Chaotic Cupid server is running. Connect the console client to /cupid.");

app.Run();
