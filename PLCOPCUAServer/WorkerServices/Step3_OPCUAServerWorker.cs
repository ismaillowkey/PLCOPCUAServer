using Opc.Ua;
using Opc.Ua.Configuration;
using PLCOPCUAServer.Shared;

namespace PLCOPCUAServer.WorkerServices;

public class Step3_OPCUAServerWorker : BackgroundService
{
    private CustomServer _server;
    private GenericNodeManager _nodeManager;
    private readonly ILogger<Step3_OPCUAServerWorker> _logger;
    private readonly int Port = 4840;

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

    private async Task StartOpcUaServerAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("3b. Inisialisasi konfigurasi OPC UA Server...");

        string hostName = System.Net.Dns.GetHostName().ToLower(); // Paksa lowercase biar konsisten
        string myAppName = "SASOpcServer";

        var config = new ApplicationConfiguration()
        {
            ApplicationName = myAppName,
            ApplicationType = ApplicationType.Server,
            // KUNCINYA: Jangan pake localhost, pake hostname asli
            ApplicationUri = $"urn:{hostName}:{myAppName}",
            ProductUri = $"uri:vmtech-sas-tangerang.com:{myAppName}",
            TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = "Directory",
                    StorePath = "OPC Foundation/CertificateStore/MachineDefault",
                    SubjectName = $"CN={myAppName}, DC={hostName}"
                },
                TrustedIssuerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = "OPC Foundation/CertificateStore/Issuer" },
                TrustedPeerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = "OPC Foundation/CertificateStore/TrustedPeer" },
                RejectedCertificateStore = new CertificateTrustList { StoreType = "Directory", StorePath = "OPC Foundation/CertificateStore/Rejected" },
                AutoAcceptUntrustedCertificates = true,
                AddAppCertToTrustedStore = true
            },
            ServerConfiguration = new ServerConfiguration()
            {
                BaseAddresses = { $"opc.tcp://{hostName}:{Port}" },
                SecurityPolicies = { new ServerSecurityPolicy { SecurityMode = MessageSecurityMode.None, SecurityPolicyUri = SecurityPolicies.None } },
                UserTokenPolicies = { new UserTokenPolicy(UserTokenType.Anonymous) },
                MaxSessionCount = 100,
                MinSessionTimeout = 10000,
                MaxSessionTimeout = 3600000,
            }
        };

        // 1. Validasi Konfigurasi
        await config.ValidateAsync(ApplicationType.Server);

        // 2. Inisialisasi Application Instance
        var application = new ApplicationInstance
        {
            ApplicationName = myAppName,
            ApplicationType = ApplicationType.Server,
            ApplicationConfiguration = config
        };

        // 3. Check & Create Certificate (WAJIB SILENT = TRUE)
        // Kalau silent = false, dia bakal nyoba buka jendela dialog dan error di Background Service
        bool hasCertificate = await application.CheckApplicationInstanceCertificatesAsync(true, 2048);

        if (!hasCertificate)
        {
            _logger.LogError("Gagal memvalidasi atau membuat sertifikat OPC UA!");
            throw new Exception("Sertifikat aplikasi tidak valid atau tidak bisa dibuat.");
        }

        _server = new CustomServer(m => _nodeManager = m);

        // Start Server
        await application.StartAsync(_server);

        Console.WriteLine($"3c. OPC UA has Started at: opc.tcp://{hostName}:{Port}");
    }
}
