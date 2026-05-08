using PLCOPCUAServer.Models;
using PLCOPCUAServer.Shared;
using static PLCOPCUAServer.Shared.GlobalData;

namespace PLCOPCUAServer.WorkerServices;

public class Step2_ReadTag : BackgroundService
{
    private readonly ILogger<Step2_ReadTag> _logger;
    private readonly Random _rnd = new Random();

    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(3));

    public Step2_ReadTag(ILogger<Step2_ReadTag> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // tunggu sampe step 1 selesai
        while (!GlobalData.Step1_AfterReadMasterTag && !stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        if (stoppingToken.IsCancellationRequested) return;

        // simulasi kalo semua plc udah konek
        foreach (var dcId in GlobalData.MachineStates.Keys)
        {
            GlobalData.PlcFirstConnectFlags[dcId] = true;
        }
        Console.WriteLine("2b. Semua flag FirstConnect udah TRUE.");

        // generate random data
        try
        {
            while (await _periodicTimer.WaitForNextTickAsync(stoppingToken))
            {
                UpdateAllDataRandomly();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error di loop Periodic Timer Step 2");
        }
    }

    private void UpdateAllDataRandomly()
    {
        // update data random dan set data di global data
        foreach (var machine in GlobalData.MachineStates)
        {
            foreach (var tag in machine.Value)
            {
                tag.TimestampRead = DateTime.Now;
                // Casting int ke Enum
                var type = (PlcDataType)tag.DataTypeId;
                tag.Value = GenerateRandomValue(type);
                Console.WriteLine($"DC {tag.LineNo}-{tag.DcNo} {tag.TagName}: {tag.Value} {tag.TimestampRead}");
            }
        }
    }

    private object GenerateRandomValue(PlcDataType type)
    {
        return type switch
        {
            PlcDataType.Boolean => _rnd.Next(0, 2) == 1,
            PlcDataType.Int16 => (short)_rnd.Next(-100, 100),
            PlcDataType.UInt16 => (ushort)_rnd.Next(0, 500),
            PlcDataType.Int32 => _rnd.Next(1000, 9999),
            PlcDataType.UInt32 => (uint)_rnd.Next(10000, 99999),
            PlcDataType.Int64 => (long)_rnd.Next(100000, 999999),
            PlcDataType.UInt64 => (ulong)_rnd.Next(100000, 999999),
            PlcDataType.Float32 or PlcDataType.Float64 => (float)Math.Round(10.0 + _rnd.NextDouble() * 50, 2),
            PlcDataType.String => "PART-" + _rnd.Next(100, 999),
            _ => 0
        };
    }
}