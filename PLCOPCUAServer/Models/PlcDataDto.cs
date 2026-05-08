using System;
using System.Collections.Generic;
using System.Text;

namespace PLCOPCUAServer.Models
{
    public class PlcDataDto
    {
        public int AddressId { get; set; }
        public int LineNo { get; set; }
        public int DcNo { get; set; }
        public DateTime TimestampRead { get; set; }
        public string TagName { get; set; }
        public object Value { get; set; }
        public int DataTypeId { get; set; }
    }
}
