using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MotronicCommunication
{
    public enum FlashState : int
    {
        Idle,
        StartErase,
        WaitEraseComplete,
        EraseError,
        StartFlashing,
        SendFlashData,
        WaitFlashData,
        SendEndFlash,
        WaitEndFlash,
        FlashingDone,
        FlashingError,
        WaitForFinishFirstBank, // M4.4 support
        SwitchBank,         // M4.4 support
        WaitBankSwitch,     // M4.4 support
        SendFlashDataUpper, // M4.4 support
        VerifyChecksum,     // M4.4 support
        WaitChecksumResults, // M4.4 support
        WaitAckForNextBank, // M4.4 support
        WaitChecksumResultsAfterFlashing, // M4.4 support
        WaitEndFlashUpperBank   // M4.4 support
    }

    public enum RxMsgType : int
    {
        StartErasingFlash,
        FinishedErasingFlash,
        FinishedFlashing,
        Acknowledge,
        NegativeAcknowledge,
        Unknown,
        StartSwitchingBank,     // M4.4 support
        FinishedSwitchingBank,   // M4.4 support
        ProgrammingVoltageOutOfRange, // M4.4 support
        NoMagicWordReceived, //M4.4 support
        StartChecksumVerification,  //M4.4 support
        FinishChecksumVerificationOK,  //M4.4 support
        FinishChecksumVerificationFailed,  //M4.4 support
        OutOfRangeError //M4.4 support
    }

    abstract public class IFlasher
    {
        abstract public void FlashFile(string filename, string comportnumber);
        abstract public void VerifyChecksum(string filename, string comportnumber);
        public delegate void StatusChanged(object sender, StatusEventArgs e);
        abstract public event StatusChanged onStatusChanged;
        public class StatusEventArgs : System.EventArgs
        {
            private string _info;

            public string Info
            {
                get { return _info; }
                set { _info = value; }
            }

            private int _percentage;

            public int Percentage
            {
                get { return _percentage; }
                set { _percentage = value; }
            }

            public StatusEventArgs(string info, int percentage)
            {
                this._info = info;
                this._percentage = percentage;
            }
        }
    }
}
