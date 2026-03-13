using Amazon.Runtime;
using Amazon.S3;
using OptimusFrame.Transform.Application;
using OptimusFrame.Transform.Infrastructure;
using OptimusFrame.Transform.Worker;
using OptimusFrame.Transform.Worker.Configuration;

var builder = Host.CreateApplicationBuilder(args);

// Configuração do RabbitMQ
builder.Services.Configure<RabbitMqSettings>(
builder.Configuration.GetSection(RabbitMqSettings.SectionName));

// Configuração de Storage (S3)
builder.Services.Configure<StorageSettings>(
builder.Configuration.GetSection(StorageSettings.SectionName));

var awsOptions = builder.Configuration.GetAWSOptions();

var accessKey = builder.Configuration["AccessKey"];
var secretKey = builder.Configuration["SecretKey"];
var sessionToken = builder.Configuration["SessionToken"];

awsOptions.Credentials = new SessionAWSCredentials(
    accessKey,
    secretKey,
    sessionToken
);

builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddAWSService<IAmazonS3>();

// Configuração limpa seguindo Clean Architecture
builder.Services
    .AddApplication()    // Registra Use Cases
    .AddInfrastructure();  // Registra implementações de Infrastructure

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
