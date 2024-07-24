namespace FastLZMA2Net
{
    public enum FL2ErrorCode
    {
        no_error = 0,
        GENERIC = 1,
        internal_error = 2,
        corruption_detected = 3,
        checksum_wrong = 4,
        parameter_unsupported = 5,
        parameter_outOfBound = 6,
        lclpMax_exceeded = 7,
        stage_wrong = 8,
        init_missing = 9,
        memory_allocation = 10,
        dstSize_tooSmall = 11,
        srcSize_wrong = 12,
        canceled = 13,
        buffer = 14,
        timedOut = 15,
        maxCode = 20  /* never EVER use this value directly, it can change in future versions! Use FL2_isError() instead */
    }

    public class FL2Exception : Exception
    {
        public FL2ErrorCode ErrorCode;

        public FL2Exception(nuint code) : base(GetErrorString(GetErrorCode(code)))
        {
            ErrorCode = GetErrorCode(code);
        }

        public static FL2ErrorCode GetErrorCode(nuint code)
        {
            if (!IsError(code))
                return FL2ErrorCode.no_error;
            return (FL2ErrorCode)(0 - code);
        }

        public static bool IsError(nuint code) => code > 0 - Enum.GetValues(typeof(FL2ErrorCode)).Cast<uint>().Max();

        public static string GetErrorString(FL2ErrorCode code) => code switch
        {
            FL2ErrorCode.no_error => "No error detected",
            FL2ErrorCode.GENERIC => "Error (generic)",
            FL2ErrorCode.internal_error => "Internal error (bug)",
            FL2ErrorCode.corruption_detected => "Corrupted block detected",
            FL2ErrorCode.checksum_wrong => "Restored data doesn't match checksum",
            FL2ErrorCode.parameter_unsupported => "Unsupported parameter",
            FL2ErrorCode.parameter_outOfBound => "Parameter is out of bound",
            FL2ErrorCode.lclpMax_exceeded => "Parameters lc+lp > 4",
            FL2ErrorCode.stage_wrong => "Not possible at this stage of encoding",
            FL2ErrorCode.init_missing => "Context should be init first",
            FL2ErrorCode.memory_allocation => "Allocation error => not enough memory",
            FL2ErrorCode.dstSize_tooSmall => "Destination buffer is too small",
            FL2ErrorCode.srcSize_wrong => "Src size is incorrect",
            FL2ErrorCode.canceled => "Processing was canceled by a call to FL2_cancelCStream() or FL2_cancelDStream()",
            FL2ErrorCode.buffer => "Streaming progress halted due to buffer(s) full/empty",
            FL2ErrorCode.timedOut => "Wait timed out. Timeouts should be handled before errors using FL2_isTimedOut()",
            /* following error codes are not stable and may be removed or changed in a future version */
            FL2ErrorCode.maxCode => "",
            _ => "Unspecified error code",
        };
    }
}