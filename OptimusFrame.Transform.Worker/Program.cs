using OptimusFrame.Transform.Application;
using OptimusFrame.Transform.Infrastructure;
using OptimusFrame.Transform.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Configuração limpa seguindo Clean Architecture
builder.Services
    .AddApplication()      // Registra Use Cases
    .AddInfrastructure();  // Registra implementações de Infrastructure

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
