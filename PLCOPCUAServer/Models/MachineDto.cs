using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLCOPCUAServer.Models
{
    public class MachineDto
    {
        public int DcNo { get; set; }        // ID unik mesin (contoh: 1, 2, 10, 11)
        public int LineNo { get; set; }      // Nomor Line (contoh: 1, 2)
        public string IpAddress { get; set; } // IP PLC (buat HSL nanti)
        public string MachineName { get; set; } // Nama mesin (optional)
    }
}
