using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Midi.Messages
{
    public enum MidiController : byte
    {
        BankSelect = 0x00,
        ModWheel = 0x01,
        Breath = 0x02,
        // 0x03 undefined
        Foot = 0x04,
        PortamentoTime = 0x05,
        DataEntry = 0x06,
        ChannelVolume = 0x07,
        Balance = 0x08,
        // 0x09 undefined
        Pan = 0x0a,
        Expression = 0x0b,
        Effect1 = 0x0c,
        Effect2 = 0x0d,
        // 0x0e undefined
        // 0x0f undefined
        GeneralPurpose1 = 0x10,
        GeneralPurpose2 = 0x11,
        GeneralPurpose3 = 0x12,
        GeneralPurpose4 = 0x13,
        // 0x14-0x1f undefined
        BankSelectLSB = 0x20,
        ModWheelLSB = 0x21,
        BreathLSB = 0x22,
        // 0x23 undefined
        FootLSB = 0x24,
        PortamentoTimeLSB = 0x25,
        DataEntryLSB = 0x26,
        ChannelVolumeLSB = 0x27,
        BalanceLSB = 0x28,
        // 0x29 undefined
        PanLSB = 0x2a,
        ExpressionLSB = 0x2b,
        Effect1LSB = 0x2c,
        Effect2LSB = 0x2d,
        // 0x2e undefined
        // 0x2f undefined
        GeneralPurpose1LSB = 0x30,
        GeneralPurpose2LSB = 0x31,
        GeneralPurpose3LSB = 0x32,
        GeneralPurpose4LSB = 0x33,
        // 0x34-0x3f undefined
        Sustain = 0x40,
        Portamento = 0x41,
        Sostenuto = 0x42,
        SoftPedal = 0x43,
        LegatoFootswitch = 0x44,
        Hold2 = 0x45,
        Sound1 = 0x46,
        Sound2 = 0x47,
        Sound3 = 0x48,
        Sound4 = 0x49,
        Sound5 = 0x4a,
        Sound6 = 0x4b,
        Sound7 = 0x4c,
        Sound8 = 0x4d,
        Sound9 = 0x4e,
        Sound10 = 0x4f,
        GeneralPurpose5 = 0x50,
        GeneralPurpose6 = 0x51,
        GeneralPurpose7 = 0x52,
        GeneralPurpose8 = 0x53,
        PortamentoControl = 0x54,
        // 0x55-0x57 undefined
        HighResolutionVelocityPrefix = 0x58,
        // 0x59-0x5a undefined
        Effects1Depth = 0x5b,
        Effects2Depth = 0x5c,
        Effects3Depth = 0x5d,
        Effects4Depth = 0x5e,
        Effects5Depth = 0x5f,
        DataIncrement = 0x60,
        DataDecrement = 0x61,
        NrpnLSB = 0x62,
        NrpnMSB = 0x63,
        RpnLSB = 0x64,
        RpnMSB = 0x65,
        // 0x66-0x77 undefined
        // 0x78- = channel modes
    }
}
