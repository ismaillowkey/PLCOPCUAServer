using PLCOPCUAServer;
using PLCOPCUAServer.WorkerServices;

var builder = Host.CreateApplicationBuilder(args);

// read master tag
builder.Services.AddHostedService<Step1_ReadMasterTagWorker>();

// Daftarkan ReadTag agar bisa di-start
builder.Services.AddHostedService<Step2_ReadTag>();

// Daftarkan Worker Service (OPC UA Server)
builder.Services.AddHostedService<Step3_OPCUAServerWorker>();

var host = builder.Build();


host.Run();