using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp13
{
    public class Values
    {
        public byte[,] DataRes; //индекс 1-номер пакета, индекс 2-номер байта
        public string Name; //2 символа 
        public int PocCount; //4 байта
        public int PocNum; //4 байта
        public ushort LengthData;//2 байта
        public int DataType; //4 байта
        public int SenderName; //4 байта
        public int ReciverName; //4 байта
        public byte[] Data;//до 600 байт
        public ushort CRS; //2 байта
        public byte[] paket;
        public byte[] CRSpaket;
    }
}
