using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace GlassRobot
{
    public class ShareMem
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr CreateFileMapping(int hFile, IntPtr lpAttributes, uint flProtect, uint dwMaxSizeHi, uint dwMaxSizeLow, string lpName);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr OpenFileMapping(int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, string lpName);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr MapViewOfFile(IntPtr hFileMapping, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool UnmapViewOfFile(IntPtr pvBaseAddress);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32", EntryPoint = "GetLastError")]
        public static extern int GetLastError();

        //定义常量
        const int ERROR_ALREADY_EXISTS = 183;
        const int INVALID_HANDLE_VALUE = -1;

        const int FILE_MAP_COPY = 0x0001;  //文件属性
        const int FILE_MAP_WRITE = 0x0002;
        const int FILE_MAP_READ = 0x0004;
        const int FILE_MAP_ALL_ACCESS = 0x0002 | 0x0004;

        const int PAGE_READONLY = 0x02;  //Map属性
        const int PAGE_READWRITE = 0x04;
        const int PAGE_WRITECOPY = 0x08;
        const int PAGE_EXECUTE = 0x10;
        const int PAGE_EXECUTE_READ = 0x20;
        const int PAGE_EXECUTE_READWRITE = 0x40;

        const int SEC_COMMIT = 0x8000000;
        const int SEC_IMAGE = 0x1000000;
        const int SEC_NOCACHE = 0x10000000;
        const int SEC_RESERVE = 0x4000000;

        IntPtr m_hSharedMemoryFileWrite = IntPtr.Zero;  //写内存文件指针
        IntPtr m_hSharedMemoryFileRead = IntPtr.Zero;  //读内存文件指针
        IntPtr m_pwDataWrite = IntPtr.Zero;  //写数据指针
        IntPtr m_pwDataRead = IntPtr.Zero;  //读数据指针
        bool m_bAlreadyExist = false;
        bool m_bInit = false;
        long m_MemSizeW = 0;  //写数据长度
        long m_MemSizeR = 0;  //读数据长度

        public ShareMem()
        {

        }

        ~ShareMem()
        {
            Close();
        }

        /// <summary>
        /// 初始化共享内存
        /// </summary>
        /// <param name="strName">共享内存名称</param>
        /// <param name="lngSize">共享内存大小</param>
        /// <returns></returns>
        public int Init(string strName, long lngSize)
        {
            if (lngSize <= 0 || lngSize > 0x00800000) lngSize = 0x00800000;  //最大8M
            m_MemSizeW = lngSize;
            if (strName.Length > 0)
            {
                //创建内存共享体(INVALID_HANDLE_VALUE)
                m_hSharedMemoryFileWrite = CreateFileMapping(INVALID_HANDLE_VALUE, IntPtr.Zero, (uint)PAGE_READWRITE, 0, (uint)lngSize, strName);
                if (m_hSharedMemoryFileWrite == IntPtr.Zero)
                {
                    m_bAlreadyExist = false;
                    m_bInit = false;
                    return 2; //创建共享体失败
                }
                else
                {
                    if (GetLastError() == ERROR_ALREADY_EXISTS)  //已经创建，会指向现有文件
                    {
                        m_bAlreadyExist = true;
                    }
                    else                                         //新创建
                    {
                        m_bAlreadyExist = false;
                    }
                }
                //---------------------------------------
                //创建内存映射
                m_pwDataWrite = MapViewOfFile(m_hSharedMemoryFileWrite, FILE_MAP_WRITE, 0, 0, (uint)lngSize);
                if (m_pwDataWrite == IntPtr.Zero)
                {
                    m_bInit = false;
                    CloseHandle(m_hSharedMemoryFileWrite);
                    return 3; //创建内存映射失败
                }
                else
                {
                    m_bInit = true;
                    if (m_bAlreadyExist == false)
                    {
                        //初始化
                    }
                }
                //----------------------------------------
            }
            else
            {
                return 1; //参数错误（文件名不正确）     
            }

            return 0;     //创建成功
        }

        /// <summary>
        /// 打开现有共享内存
        /// </summary>
        /// <param name="strName">共享内存名称</param>
        /// <param name="lngSize">共享内存大小</param>
        /// <returns></returns>
        public int Open(string strName, long lngSize)
        {
            if (strName.Length > 0)
            {
                //打开内存共享体(INVALID_HANDLE_VALUE)
                m_hSharedMemoryFileRead = OpenFileMapping(FILE_MAP_READ, false, strName);
                if (m_hSharedMemoryFileRead == IntPtr.Zero)
                {
                    return 2; //打开共享体失败
                }
                //---------------------------------------
                //创建内存映射
                m_pwDataRead = MapViewOfFile(m_hSharedMemoryFileRead, FILE_MAP_READ, 0, 0, (uint)lngSize);
                if (m_pwDataRead == IntPtr.Zero)
                {
                    CloseHandle(m_hSharedMemoryFileRead);
                    return 3; //创建内存映射失败
                }
                //----------------------------------------
            }
            else
            {
                return 1; //参数错误（文件名不正确）
            }

            return 0;     //创建成功
        }

        /// <summary>
        /// 关闭共享内存
        /// </summary>
        public void Close()
        {
            if (m_bInit)  //只关闭自己建立的共享内存
            {
                UnmapViewOfFile(m_pwDataWrite);
                UnmapViewOfFile(m_pwDataRead);
                CloseHandle(m_hSharedMemoryFileWrite);
            }
        }

        /// <summary>
        /// 读数据
        /// </summary>
        /// <param name="bytData">数据</param>
        /// <param name="lngAddr">起始地址</param>
        /// <param name="lngSize">个数</param>
        /// <returns></returns>
        public int Read(byte[] bytData, int lngAddr, int lngSize)
        {
            if (lngAddr + lngSize > m_MemSizeW) return 2; //超出数据区

            Marshal.Copy(m_pwDataRead, bytData, lngAddr, lngSize);
            
            return 0;     //读成功
        }

        /// <summary>
        /// 写数据
        /// </summary>
        /// <param name="bytData">数据</param>
        /// <param name="lngAddr">起始地址</param>
        /// <param name="lngSize">个数</param>
        /// <returns></returns>
        public int Write(byte[] bytData, int lngAddr, int lngSize)
        {
            if (lngAddr + lngSize > m_MemSizeR) return 2; //超出数据区
            if (!m_bInit) return 1;; //共享内存未初始化
                        
            Marshal.Copy(bytData, lngAddr, m_pwDataWrite, lngSize);
            
            return 0;     //写成功
        }
    }
}