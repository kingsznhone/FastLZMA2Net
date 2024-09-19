namespace FastLZMA2Net
{
    public enum FL2ErrorCode
    {
        NoError = 0,
        Generic = 1,
        InternalError = 2,
        CorruptionDetected = 3,
        ChecksumWrong = 4,
        ParameterUnsupported = 5,
        ParameterOutOfBound = 6,
        LclpMaxExceeded = 7,
        StageWrong = 8,
        InitMissing = 9,
        MemoryAllocation = 10,
        DstSizeTooSmall = 11,
        SrcSizeWrong = 12,
        Canceled = 13,
        Buffer = 14,
        TimedOut = 15,
        MaxCode = 20  /* never EVER use this value directly, it can change in future versions! Use FL2_isError() instead */
    }

    public class FL2Exception : Exception
    {
        private readonly FL2ErrorCode _errorCode;
        public FL2ErrorCode ErrorCode => _errorCode;
        public FL2Exception(nuint code) : base(GetErrorString(GetErrorCode(code)))
        {
            _errorCode = GetErrorCode(code);
        }

        public FL2Exception(FL2ErrorCode code) : base(GetErrorString(code))
        {
            _errorCode = code;
        }

        public static FL2ErrorCode GetErrorCode(nuint code)
        {
            if (!IsError(code))
                return FL2ErrorCode.NoError;
            return (FL2ErrorCode)(0 - code);
        }

        public static bool IsError(nuint code) => code > 0 - Enum.GetValues(typeof(FL2ErrorCode)).Cast<uint>().Max();

        public static string GetErrorString(FL2ErrorCode code) => code switch
        {
            FL2ErrorCode.NoError => "No error detected",
            FL2ErrorCode.Generic => "Error (generic)",
            FL2ErrorCode.InternalError => "Internal error (bug)",
            FL2ErrorCode.CorruptionDetected => "Corrupted block detected",
            FL2ErrorCode.ChecksumWrong => "Restored data doesn't match checksum",
            FL2ErrorCode.ParameterUnsupported => "Unsupported parameter",
            FL2ErrorCode.ParameterOutOfBound => "Parameter is out of bound",
            FL2ErrorCode.LclpMaxExceeded => "Parameters lc+lp > 4",
            FL2ErrorCode.StageWrong => "Not possible at this stage of encoding",
            FL2ErrorCode.InitMissing => "Context should be init first",
            FL2ErrorCode.MemoryAllocation => "Allocation error => not enough memory",
            FL2ErrorCode.DstSizeTooSmall => "Destination buffer is too small",
            FL2ErrorCode.SrcSizeWrong => "Src size is incorrect",
            FL2ErrorCode.Canceled => "Processing was canceled by a call to FL2_cancelCStream() or FL2_cancelDStream()",
            FL2ErrorCode.Buffer => "Streaming progress halted due to buffer(s) full/empty",
            FL2ErrorCode.TimedOut => "Wait timed out. Timeouts should be handled before errors using FL2_isTimedOut()",
            /* following error codes are not stable and may be removed or changed in a future version */
            FL2ErrorCode.MaxCode => "",
            _ => "Unspecified error code",
        };


    }
}