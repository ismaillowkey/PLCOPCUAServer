using Opc.Ua;
using Opc.Ua.Configuration;
using PLCOPCUAServer.Shared;

namespace PLCOPCUAServer.WorkerServices;

public class Step3_OPCUAServerWorker : BackgroundService
{
    private CustomServer _server;
    private GenericNodeManager _nodeManager;
    private readonly ILogger<Step3_OPCUAServerWorker> _logger;
    private readonly int Port = 4480;

    public Step3_OPCUAServerWorker(ILogger<Step3_OPCUAServerWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!GlobalData.Step2_AllPLCFirstConnect && !stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(2000, stoppingToken);
        }

        if (stoppingToken.IsCancellationRequested) return;

        Console.WriteLine("3a. OPC UA Server: starting...");

        try
        {
            await StartOpcUaServerAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                _nodeManager?.SyncValues();
                await Task.Delay(500, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error pada OPC UA Server Worker");
        }
        finally
        {
            _server?.StopAsync();
            _logger.LogInformation("OPC UA Server stopped.");
        }
    }

    // --- FUNGSI BARU: URUSAN TEKNIS CONFIG & START ---
    private async Task StartOpcUaServerAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("3b. Inisialisasi konfigurasi OPC UA Server...");

        var config = new ApplicationConfiguration()
        {
            ApplicationName = "IndustrialOpcServer",
            ApplicationType = ApplicationType.Server,
            ApplicationUri = "urn:localhost:IndustrialOpcServer",
            ProductUri = "uri:ismail-automation.com:IndustrialOpcServer",
            TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = "Directory",
                    StorePath = "OPC Foundation/CertificateStore/MachineDefault",
                    SubjectName = "CN=IndustrialOpcServer"
                },
                TrustedIssuerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = "OPC Foundation/CertificateStore/Issuer" },
                TrustedPeerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = "OPC Foundation/CertificateStore/TrustedPeer" },
                RejectedCertificateStore = new CertificateTrustList { StoreType = "Directory", StorePath = "OPC Foundation/CertificateStore/Rejected" },
                AutoAcceptUntrustedCertificates = true,
                AddAppCertToTrustedStore = true
            },
            ServerConfiguration = new ServerConfiguration()
            {
                BaseAddresses = { $"opc.tcp://0.0.0.0:{Port}" },
                SecurityPolicies = { new ServerSecurityPolicy { SecurityMode = MessageSecurityMode.None, SecurityPolicyUri = SecurityPolicies.None } },
                UserTokenPolicies = { new UserTokenPolicy(UserTokenType.Anonymous) },
                MaxSessionCount = 100,
                MinSessionTimeout = 10000,
                MaxSessionTimeout = 3600000,
                MaxBrowseContinuationPoints = 10,
                MaxQueryContinuationPoints = 10,
                MaxHistoryContinuationPoints = 100,
                MaxRequestAge = 600000,
                MinRequestThreadCount = 5,
                MaxRequestThreadCount = 100,
                MaxQueuedRequestCount = 200
            }
        };

        await config.ValidateAsync(ApplicationType.Server);

        var application = new ApplicationInstance((ITelemetryContext)null)
        {
            ApplicationName = "IndustrialOpcServer",
            ApplicationType = ApplicationType.Server,
            ApplicationConfiguration = config
        };

        bool hasCertificate = await application.CheckApplicationInstanceCertificatesAsync(false, 2048);
        if (!hasCertificate)
        {
            _logger.LogWarning("Sertifikat baru dibuat.");
        }

        // Inisialisasi CustomServer
        _server = new CustomServer(m => _nodeManager = m);
        Console.WriteLine($"3c. OPC UA has Started at port: {Port}");
        await application.StartAsync(_server);
    }
}