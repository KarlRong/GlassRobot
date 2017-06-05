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

        //���峣��
        const int ERROR_ALREADY_EXISTS = 183;
        const int INVALID_HANDLE_VALUE = -1;

        const int FILE_MAP_COPY = 0x0001;  //�ļ�����
        const int FILE_MAP_WRITE = 0x0002;
        const int FILE_MAP_READ = 0x0004;
        const int FILE_MAP_ALL_ACCESS = 0x0002 | 0x0004;

        const int PAGE_READONLY = 0x02;  //Map����
        const int PAGE_READWRITE = 0x04;
        const int PAGE_WRITECOPY = 0x08;
        const int PAGE_EXECUTE = 0x10;
        const int PAGE_EXECUTE_READ = 0x20;
        const int PAGE_EXECUTE_READWRITE = 0x40;

        const int SEC_COMMIT = 0x8000000;
        const int SEC_IMAGE = 0x1000000;
        const int SEC_NOCACHE = 0x10000000;
        const int SEC_RESERVE = 0x4000000;

        IntPtr m_hSharedMemoryFileWrite = IntPtr.Zero;  //д�ڴ��ļ�ָ��
        IntPtr m_hSharedMemoryFileRead = IntPtr.Zero;  //���ڴ��ļ�ָ��
        IntPtr m_pwDataWrite = IntPtr.Zero;  //д����ָ��
        IntPtr m_pwDataRead = IntPtr.Zero;  //������ָ��
        bool m_bAlreadyExist = false;
        bool m_bInit = false;
        long m_MemSizeW = 0;  //д���ݳ���
        long m_MemSizeR = 0;  //�����ݳ���

        public ShareMem()
        {

        }

        ~ShareMem()
        {
            Close();
        }

        /// <summary>
        /// ��ʼ�������ڴ�
        /// </summary>
        /// <param name="strName">�����ڴ�����</param>
        /// <param name="lngSize">�����ڴ��С</param>
        /// <returns></returns>
        public int Init(string strName, long lngSize)
        {
            if (lngSize <= 0 || lngSize > 0x00800000) lngSize = 0x00800000;  //���8M
            m_MemSizeW = lngSize;
            if (strName.Length > 0)
            {
                //�����ڴ湲����(INVALID_HANDLE_VALUE)
                m_hSharedMemoryFileWrite = CreateFileMapping(INVALID_HANDLE_VALUE, IntPtr.Zero, (uint)PAGE_READWRITE, 0, (uint)lngSize, strName);
                if (m_hSharedMemoryFileWrite == IntPtr.Zero)
                {
                    m_bAlreadyExist = false;
                    m_bInit = false;
                    return 2; //����������ʧ��
                }
                else
                {
                    if (GetLastError() == ERROR_ALREADY_EXISTS)  //�Ѿ���������ָ�������ļ�
                    {
                        m_bAlreadyExist = true;
                    }
                    else                                         //�´���
                    {
                        m_bAlreadyExist = false;
                    }
                }
                //---------------------------------------
                //�����ڴ�ӳ��
                m_pwDataWrite = MapViewOfFile(m_hSharedMemoryFileWrite, FILE_MAP_WRITE, 0, 0, (uint)lngSize);
                if (m_pwDataWrite == IntPtr.Zero)
                {
                    m_bInit = false;
                    CloseHandle(m_hSharedMemoryFileWrite);
                    return 3; //�����ڴ�ӳ��ʧ��
                }
                else
                {
                    m_bInit = true;
                    if (m_bAlreadyExist == false)
                    {
                        //��ʼ��
                    }
                }
                //----------------------------------------
            }
            else
            {
                return 1; //���������ļ�������ȷ��     
            }

            return 0;     //�����ɹ�
        }

        /// <summary>
        /// �����й����ڴ�
        /// </summary>
        /// <param name="strName">�����ڴ�����</param>
        /// <param name="lngSize">�����ڴ��С</param>
        /// <returns></returns>
        public int Open(string strName, long lngSize)
        {
            if (strName.Length > 0)
            {
                //���ڴ湲����(INVALID_HANDLE_VALUE)
                m_hSharedMemoryFileRead = OpenFileMapping(FILE_MAP_READ, false, strName);
                if (m_hSharedMemoryFileRead == IntPtr.Zero)
                {
                    return 2; //�򿪹�����ʧ��
                }
                //---------------------------------------
                //�����ڴ�ӳ��
                m_pwDataRead = MapViewOfFile(m_hSharedMemoryFileRead, FILE_MAP_READ, 0, 0, (uint)lngSize);
                if (m_pwDataRead == IntPtr.Zero)
                {
                    CloseHandle(m_hSharedMemoryFileRead);
                    return 3; //�����ڴ�ӳ��ʧ��
                }
                //----------------------------------------
            }
            else
            {
                return 1; //���������ļ�������ȷ��
            }

            return 0;     //�����ɹ�
        }

        /// <summary>
        /// �رչ����ڴ�
        /// </summary>
        public void Close()
        {
            if (m_bInit)  //ֻ�ر��Լ������Ĺ����ڴ�
            {
                UnmapViewOfFile(m_pwDataWrite);
                UnmapViewOfFile(m_pwDataRead);
                CloseHandle(m_hSharedMemoryFileWrite);
            }
        }

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="bytData">����</param>
        /// <param name="lngAddr">��ʼ��ַ</param>
        /// <param name="lngSize">����</param>
        /// <returns></returns>
        public int Read(byte[] bytData, int lngAddr, int lngSize)
        {
            if (lngAddr + lngSize > m_MemSizeW) return 2; //����������

            Marshal.Copy(m_pwDataRead, bytData, lngAddr, lngSize);
            
            return 0;     //���ɹ�
        }

        /// <summary>
        /// д����
        /// </summary>
        /// <param name="bytData">����</param>
        /// <param name="lngAddr">��ʼ��ַ</param>
        /// <param name="lngSize">����</param>
        /// <returns></returns>
        public int Write(byte[] bytData, int lngAddr, int lngSize)
        {
            if (lngAddr + lngSize > m_MemSizeR) return 2; //����������
            if (!m_bInit) return 1;; //�����ڴ�δ��ʼ��
                        
            Marshal.Copy(bytData, lngAddr, m_pwDataWrite, lngSize);
            
            return 0;     //д�ɹ�
        }
    }
}