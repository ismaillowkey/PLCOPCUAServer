using PLCOPCUAServer.Models;
using PLCOPCUAServer.Shared;

namespace PLCOPCUAServer.WorkerServices;

public class Step1_ReadMasterTagWorker : BackgroundService
{
    private readonly ILogger<Step1_ReadMasterTagWorker> _logger;

    public Step1_ReadMasterTagWorker(ILogger<Step1_ReadMasterTagWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReadMasterTagWorker: Memulai inisialisasi konfigurasi...");

        try
        {
            ReadMasterData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gagal melakukan inisialisasi Master Data.");
        }

        // Karena ini cuma inisialisasi, worker ini bisa berhenti atau standby
        // Kalo lu mau dukung update DB tanpa restart, lu bisa looping tiap 1 jam di sini.
        await Task.CompletedTask;
    }

    private void ReadMasterData()
    {
        // 1. get master data tag
        Console.WriteLine("1a. Get Master data");
        GlobalData.MasterTagTemplate = FetchMasterTagsFromDb();

        // 2. get List Machine (Banyak)
        Console.WriteLine("1b. Get Master List PLC Machine");
        var machines = FetchActiveMachinesFromDb();


        // 3. Setup Laci dan Satpam
        Console.WriteLine("1c. Generate empty data for OPC UA server");
        foreach (var m in machines)
        {
            // Daftarkan DC ke sistem penunggu (FirstConnect)
            GlobalData.PlcFirstConnectFlags[m.DcNo] = false;

            // generate empty data
            var initialTags = GlobalData.MasterTagTemplate.Select(master => new PlcDataDto
            {
                AddressId = (m.DcNo * 1000) + master.Id, // Bikin unique ID gabungan DC + Tag ID
                LineNo = m.LineNo,
                DcNo = m.DcNo,
                TagName = master.TagName,
                DataTypeId = master.DataTypeId, // Ambil dari DTO master
                Value = null // Masih kosong nunggu ReadTag
            }).ToList();

            GlobalData.MachineStates[m.DcNo] = initialTags;

            Console.WriteLine($"1d. [Inisialisasi] Line {m.LineNo} - DC {m.DcNo} (IP: {m.IpAddress}) Ready.");
        }

        Console.WriteLine("1e. Seluruh konfigurasi mesin berhasil di-load.");

        GlobalData.Step1_AfterReadMasterTag = true; // set ke global data, service readtag akan run setelag ini true
    }

    private List<MasterTagDto> FetchMasterTagsFromDb()
    {
        // --- EDIT DI SINI UNTUK DB QUERY ---
        return new List<MasterTagDto>
        {
            new MasterTagDto { Id = 1, TagName = "DataDiesNo", Address = "V0", DataTypeId = (int)GlobalData.PlcDataType.Int16, Length = 0, Note = "Dies No" },
            new MasterTagDto { Id = 2, TagName = "DataTotalPcs", Address = "V1", DataTypeId = (int)GlobalData.PlcDataType.Int32, Length = 0, Note = "Total Pcs" },
            new MasterTagDto { Id = 3, TagName = "DataPartName", Address = "V2", DataTypeId = (int)GlobalData.PlcDataType.String, Length = 10, Note = "Part Name" }
        };
    }

    private List<MachineDto> FetchActiveMachinesFromDb()
    {
        var listMesin = new List<MachineDto>();

        // Simulasi input banyak mesin (Line 1 ada 5 DC, Line 2 ada 5 DC)
        for (int i = 1; i <= 5; i++)
        {
            listMesin.Add(new MachineDto { DcNo = i, LineNo = 1, IpAddress = $"192.168.1.{10 + i}", MachineName = $"Mtc Line 1 DC {i}" });
        }

        for (int i = 6; i <= 10; i++)
        {
            listMesin.Add(new MachineDto { DcNo = i, LineNo = 2, IpAddress = $"192.168.1.{10 + i}", MachineName = $"Mtc Line 2 DC {i}" });
        }

        return listMesin;
    }
}