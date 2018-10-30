using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace com.veda.X4Lidar
{
    public static class GWin32
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateFile(
             [MarshalAs(UnmanagedType.LPTStr)] string filename,
             [MarshalAs(UnmanagedType.U4)] FileAccess access,
             [MarshalAs(UnmanagedType.U4)] FileShare share,
             IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
             [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
             [MarshalAs(UnmanagedType.U4)] uint flagsAndAttributes,
             IntPtr templateFile);

        [DllImport("kernel32.dll")]
        public static extern bool SetCommMask(IntPtr hFile, uint dwEvtMask);

        [DllImport("kernel32.dll")]
        public static extern bool GetCommState(IntPtr hFile, ref DCB lpDCB);
        [DllImport("kernel32.dll")]
        public static extern bool SetCommState(IntPtr hFile, ref DCB lpDCB);

        [DllImport("kernel32.dll")]
        static extern bool WaitCommEvent(IntPtr hFile, out uint lpEvtMask,
            IntPtr lpOverlapped);

        public delegate void WriteFileCompletionDelegate(UInt32 dwErrorCode,
    UInt32 dwNumberOfBytesTransfered, ref NativeOverlapped lpOverlapped);
        [DllImport("kernel32.dll")]
        public static extern bool WriteFileEx(IntPtr hFile, byte[] lpBuffer,
   uint nNumberOfBytesToWrite, [In] ref System.Threading.NativeOverlapped lpOverlapped,
   WriteFileCompletionDelegate lpCompletionRoutine);

        [DllImport("kernel32.dll")]
        public static extern bool ReadFileEx(IntPtr hFile, [Out] byte[] lpBuffer,
   uint nNumberOfBytesToRead, [In] ref System.Threading.NativeOverlapped lpOverlapped,
   WriteFileCompletionDelegate lpCompletionRoutine);

        [DllImport("kernel32.dll")]
        public static extern bool ReadFile(IntPtr hFile, [Out] byte[] lpBuffer,
   uint nNumberOfBytesToRead, out uint lpNumRead, ref NativeOverlapped lpOverlapped);

        [DllImport("kernel32.dll")]
        public static extern void SleepEx(uint dwMilliseconds, bool alertable);

        [DllImport("kernel32.dll", SetLastError = true)]
        public  static extern bool GetOverlappedResult(IntPtr hFile,
           ref System.Threading.NativeOverlapped lpOverlapped,
           out uint lpNumberOfBytesTransferred, bool bWait);

        [DllImport("kernel32.dll")]
        public static extern bool PurgeComm(IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetCommTimeouts(IntPtr hFile, [In] ref COMMTIMEOUTS lpCommTimeouts);

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();


        [DllImport("kernel32.dll")]
        public static extern void SetLastError([In] uint err);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern UInt32 WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);


        public const uint INFINITE = 0xffffffff;
        public struct COMMTIMEOUTS
        {
            public UInt32 ReadIntervalTimeout;
            public UInt32 ReadTotalTimeoutMultiplier;
            public UInt32 ReadTotalTimeoutConstant;
            public UInt32 WriteTotalTimeoutMultiplier;
            public UInt32 WriteTotalTimeoutConstant;
        }

        public enum DtrControl : int
        {
            /// <summary>
            /// Disables the DTR line when the device is opened and leaves it disabled.
            /// </summary>
            Disable = 0,

            /// <summary>
            /// Enables the DTR line when the device is opened and leaves it on.
            /// </summary>
            Enable = 1,

            /// <summary>
            /// Enables DTR handshaking. If handshaking is enabled, it is an error for the application to adjust the line by
            /// using the EscapeCommFunction function.
            /// </summary>
            Handshake = 2
        }
        public enum RtsControl : int
        {
            /// <summary>
            /// Disables the RTS line when the device is opened and leaves it disabled.
            /// </summary>
            Disable = 0,

            /// <summary>
            /// Enables the RTS line when the device is opened and leaves it on.
            /// </summary>
            Enable = 1,

            /// <summary>
            /// Enables RTS handshaking. The driver raises the RTS line when the "type-ahead" (input) buffer
            /// is less than one-half full and lowers the RTS line when the buffer is more than
            /// three-quarters full. If handshaking is enabled, it is an error for the application to
            /// adjust the line by using the EscapeCommFunction function.
            /// </summary>
            Handshake = 2,

            /// <summary>
            /// Specifies that the RTS line will be high if bytes are available for transmission. After
            /// all buffered bytes have been sent, the RTS line will be low.
            /// </summary>
            Toggle = 3
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct DCB
        {
            internal uint DCBLength;
            internal uint BaudRate;
            private BitVector32 Flags;

            private ushort wReserved;        // not currently used
            internal ushort XonLim;           // transmit XON threshold
            internal ushort XoffLim;          // transmit XOFF threshold             

            internal byte ByteSize;
            internal Parity Parity;
            internal StopBits StopBits;

            internal sbyte XonChar;          // Tx and Rx XON character
            internal sbyte XoffChar;         // Tx and Rx XOFF character
            internal sbyte ErrorChar;        // error replacement character
            internal sbyte EofChar;          // end of input character
            internal sbyte EvtChar;          // received event character
            private ushort wReserved1;       // reserved; do not use     

            private static readonly int fBinary;
            private static readonly int fParity;
            private static readonly int fOutxCtsFlow;
            private static readonly int fOutxDsrFlow;
            private static readonly BitVector32.Section fDtrControl;
            private static readonly int fDsrSensitivity;
            private static readonly int fTXContinueOnXoff;
            private static readonly int fOutX;
            private static readonly int fInX;
            private static readonly int fErrorChar;
            private static readonly int fNull;
            private static readonly BitVector32.Section fRtsControl;
            private static readonly int fAbortOnError;

            static DCB()
            {
                // Create Boolean Mask
                int previousMask;
                fBinary = BitVector32.CreateMask();
                fParity = BitVector32.CreateMask(fBinary);
                fOutxCtsFlow = BitVector32.CreateMask(fParity);
                fOutxDsrFlow = BitVector32.CreateMask(fOutxCtsFlow);
                previousMask = BitVector32.CreateMask(fOutxDsrFlow);
                previousMask = BitVector32.CreateMask(previousMask);
                fDsrSensitivity = BitVector32.CreateMask(previousMask);
                fTXContinueOnXoff = BitVector32.CreateMask(fDsrSensitivity);
                fOutX = BitVector32.CreateMask(fTXContinueOnXoff);
                fInX = BitVector32.CreateMask(fOutX);
                fErrorChar = BitVector32.CreateMask(fInX);
                fNull = BitVector32.CreateMask(fErrorChar);
                previousMask = BitVector32.CreateMask(fNull);
                previousMask = BitVector32.CreateMask(previousMask);
                fAbortOnError = BitVector32.CreateMask(previousMask);

                // Create section Mask
                BitVector32.Section previousSection;
                previousSection = BitVector32.CreateSection(1);
                previousSection = BitVector32.CreateSection(1, previousSection);
                previousSection = BitVector32.CreateSection(1, previousSection);
                previousSection = BitVector32.CreateSection(1, previousSection);
                fDtrControl = BitVector32.CreateSection(2, previousSection);
                previousSection = BitVector32.CreateSection(1, fDtrControl);
                previousSection = BitVector32.CreateSection(1, previousSection);
                previousSection = BitVector32.CreateSection(1, previousSection);
                previousSection = BitVector32.CreateSection(1, previousSection);
                previousSection = BitVector32.CreateSection(1, previousSection);
                previousSection = BitVector32.CreateSection(1, previousSection);
                fRtsControl = BitVector32.CreateSection(3, previousSection);
                previousSection = BitVector32.CreateSection(1, fRtsControl);
            }

            public bool Binary
            {
                get { return Flags[fBinary]; }
                set { Flags[fBinary] = value; }
            }

            public bool CheckParity
            {
                get { return Flags[fParity]; }
                set { Flags[fParity] = value; }
            }

            public bool OutxCtsFlow
            {
                get { return Flags[fOutxCtsFlow]; }
                set { Flags[fOutxCtsFlow] = value; }
            }

            public bool OutxDsrFlow
            {
                get { return Flags[fOutxDsrFlow]; }
                set { Flags[fOutxDsrFlow] = value; }
            }

            public DtrControl DtrControl
            {
                get { return (DtrControl)Flags[fDtrControl]; }
                set { Flags[fDtrControl] = (int)value; }
            }

            public bool DsrSensitivity
            {
                get { return Flags[fDsrSensitivity]; }
                set { Flags[fDsrSensitivity] = value; }
            }

            public bool TxContinueOnXoff
            {
                get { return Flags[fTXContinueOnXoff]; }
                set { Flags[fTXContinueOnXoff] = value; }
            }

            public bool OutX
            {
                get { return Flags[fOutX]; }
                set { Flags[fOutX] = value; }
            }

            public bool InX
            {
                get { return Flags[fInX]; }
                set { Flags[fInX] = value; }
            }

            public bool ReplaceErrorChar
            {
                get { return Flags[fErrorChar]; }
                set { Flags[fErrorChar] = value; }
            }

            public bool Null
            {
                get { return Flags[fNull]; }
                set { Flags[fNull] = value; }
            }

            public RtsControl RtsControl
            {
                get { return (RtsControl)Flags[fRtsControl]; }
                set { Flags[fRtsControl] = (int)value; }
            }

            public bool AbortOnError
            {
                get { return Flags[fAbortOnError]; }
                set { Flags[fAbortOnError] = value; }
            }
        }
    }
}
