using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using PLCOPCUAServer.Models;

namespace PLCOPCUAServer.Shared
{
    public static class GlobalData
    {
        public enum PlcDataType
        {
            Boolean = 1,
            Int16 = 2,
            UInt16 = 3,
            Int32 = 4,
            UInt32 = 5,
            Int64 = 6,
            UInt64 = 7,
            Float32 = 8,
            Float64 = 9,
            String = 10
        }

        // PILLAR 1: Master data for read every {:C
        public static List<MasterTagDto> MasterTagTemplate = new();

        // Machine state, isinya data hasil pembacaan
        // Key: DcNo(ID Mesin)
        public static ConcurrentDictionary<int, List<PlcDataDto>> MachineStates = new();

        // Status FirstConnect, jika true semua maka opc ua server akan run
        // Key: DcNo, Value: bool (True kalo udah FirstConnect)
        public static ConcurrentDictionary<int, bool> PlcFirstConnectFlags { get; set; } = new();

        public static bool Step1_AfterReadMasterTag { get; set; }
        // get PlcFirstConnectFlags, return true jika semua true
        public static bool Step2_AllPLCFirstConnect => !PlcFirstConnectFlags.IsEmpty && PlcFirstConnectFlags.Values.All(v => v);
    }
}
