using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Drivers
{
    public class Roland
    {
        // TODO: This is all just quick stuff to test handling of sysex through WinAPI
        private const byte sysexId = 0x41;

        public byte[] GenerateSysex(int address, byte[] data)
        {
            byte checksum = 0;

            byte lo_addr = (byte)(address & 0x7f);
            checksum -= lo_addr;
            address >>= 7;

            byte mid_addr = (byte)(address & 0x7f);
            checksum -= mid_addr;
            address >>= 7;

            byte hi_addr = (byte)(address & 0x7f);
            checksum -= hi_addr;
            //address >>= 7;

            byte[] buffer = new byte[data.Length + 9];

            int index = 0;
            buffer[index++] = sysexId;
            buffer[index++] = 0x10;
            buffer[index++] = 0x16;
            buffer[index++] = 0x12;
            buffer[index++] = hi_addr;
            buffer[index++] = mid_addr;
            buffer[index++] = lo_addr;

            foreach (byte b in data)
            {
                checksum -= b;
                buffer[index++] = b;
            }

            buffer[index++] = (byte)(checksum & 0x7f);
            buffer[index++] = 0xf7;

            return buffer;
        }
    }
}
