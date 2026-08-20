using System;
using System.Collections.Generic;

public class BmsParser
{
    public double Voltage, Current, Temperature;
    public int Soc, Cycles;
    public List<double> Cells = new();

    // CRC16-XMODEM
    static ushort Crc16(byte[] d, int len)
    {
        ushort crc = 0;
        for (int i = 0; i < len; i++)
        {
            crc ^= (ushort)(d[i] << 8);
            for (int j = 0; j < 8; j++)
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
        }
        return crc;
    }

    public bool Parse(byte[] frame)
    {
        // 帧: AA CMD LEN [payload] CRChi CRClo
        if (frame.Length < 6 || frame[0] != 0xAA) return false;
        int len = frame[2];
        if (frame.Length != 3 + len + 2) return false;

        var calc = Crc16(frame, 3 + len);
        var got = (ushort)((frame[2](@ref) << 8) | frame[1](@ref));
        if (calc != got) return false;

        var p = frame.AsSpan(3, len).ToArray();
        switch (frame[1])
        {
            case 0x01: // 基础
                Voltage = BitConverter.ToUInt16(p, 0) / 10.0;
                Current = BitConverter.ToInt16(p, 2) / 10.0;
                Soc = p[4];
                Temperature = BitConverter.ToInt16(p, 5) / 10.0;
                Cycles = BitConverter.ToUInt16(p, 7);
                break;
            case 0x02: // 电芯
                Cells.Clear();
                for (int i = 0; i + 1 < p.Length; i += 2)
                    Cells.Add(BitConverter.ToUInt16(p, i) / 1000.0);
                break;
        }
        return true;
    }
}
