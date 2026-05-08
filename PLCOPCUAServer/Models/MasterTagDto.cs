using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLCOPCUAServer.Models
{
    public class MasterTagDto
    {
        public int Id { get; set; }           // Primary Key di DB
        public string TagName { get; set; }   // Contoh: "DataPartName"
        public string Address { get; set; }   // Contoh: "V2" atau "D100"
        public int DataTypeId { get; set; }    // ID tipe (1: ushort, 2: int, dst)
        public int Length { get; set; }       // Khusus string (misal: 10)
        public string Note { get; set; }      // Keterangan tambahan

        // Helper buat mapping ke teks (opsional)
        public string DataTypeName { get; set; }
    }
}
