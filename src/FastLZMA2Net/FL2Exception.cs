namespace FastLZMA2Net
{
    /// <summary>
    /// Native error codes reported by the Fast LZMA2 encoder and decoder.
    /// </summary>
    public enum FL2ErrorCode
    {
        /// <summary>No error was reported.</summary>
        NoError = 0,

        /// <summary>Generic error.</summary>
        Generic = 1,

        /// <summary>Internal library error.</summary>
        InternalError = 2,

        /// <summary>Corruption was detected in the input stream.</summary>
        CorruptionDetected = 3,

        /// <summary>The decoded data checksum did not match.</summary>
        ChecksumWrong = 4,

        /// <summary>The requested parameter is not supported.</summary>
        ParameterUnsupported = 5,

        /// <summary>The requested parameter value is out of range.</summary>
        ParameterOutOfBound = 6,

        /// <summary>The LCLP limit was exceeded.</summary>
        LclpMaxExceeded = 7,

        /// <summary>The operation is not valid in the current stage.</summary>
        StageWrong = 8,

        /// <summary>The context has not been initialized.</summary>
        InitMissing = 9,

        /// <summary>Memory allocation failed.</summary>
        MemoryAllocation = 10,

        /// <summary>The destination buffer is too small.</summary>
        DstSizeTooSmall = 11,

        /// <summary>The source size is invalid.</summary>
        SrcSizeWrong = 12,

        /// <summary>The operation was canceled.</summary>
        Canceled = 13,

        /// <summary>A buffer became full or empty during streaming.</summary>
        Buffer = 14,

        /// <summary>The operation timed out.</summary>
        TimedOut = 15,

        /// <summary>Maximum stable error code value; do not use directly.</summary>
        MaxCode = 20  /* never EVER use this value directly, it can change in future versions! Use FL2_isError() instead */
    }

    /// <summary>
    /// Exception thrown when a Fast LZMA2 native operation fails.
    /// </summary>
    public class FL2Exception : Exception
    {
        private readonly FL2ErrorCode _errorCode;

        /// <summary>
        /// Gets the native error code associated with this exception.
        /// </summary>
        public FL2ErrorCode ErrorCode => _errorCode;

        internal FL2Exception(nuint code) : base(GetErrorString(GetErrorCode(code)))
        {
            _errorCode = GetErrorCode(code);
        }

        internal FL2Exception(FL2ErrorCode code) : base(GetErrorString(code))
        {
            _errorCode = code;
        }

        /// <summary>
        /// Creates an FL2Exception with a custom message and inner exception.
        /// </summary>
        /// <summary>
        /// Initializes a new instance with a custom message and inner exception.
        /// </summary>
        public FL2Exception(string message, Exception innerException) : base(message, innerException)
        {
            _errorCode = FL2ErrorCode.Generic;
        }

        internal static FL2ErrorCode GetErrorCode(nuint code)
        {
            if (!IsError(code))
                return FL2ErrorCode.NoError;
            return (FL2ErrorCode)(0 - code);
        }

        // Error codes are returned as large nuint values: -1 == nuint.MaxValue, -MaxCode == nuint.MaxValue - MaxCode + 1.
        // Cache the boundary once to avoid per-call reflection.

        private static readonly nuint _errorBound = unchecked(0u - (nuint)(uint)FL2ErrorCode.MaxCode);

        internal static bool IsError(nuint code) => code > _errorBound;

        internal static string GetErrorString(FL2ErrorCode code) => code switch
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

        /// <summary>
        /// Initializes a new instance with a custom message.
        /// </summary>
        public FL2Exception(string message) : base(message)
        {
        }
    }
}