using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Controller
{
    public class SYSPLC
    {
        //���г�Ա==============================================================================================================
        public static uint T_PLC = 100;  //PLC��������
        //5�������źżĴ���
        public static int DI = 0;
        public static int LLimit = 0;
        public static int RLimit = 0;
        public static int Home = 0;
        public static int Alarm = 0;

        //3������źżĴ���
        public static int DOR = 0, DOW = 0;  //DO״̬��DOд�Ĵ���
        public static int ServR = 0, ServW = 0;  //DO״̬��DOд�Ĵ���
        public static int ClsR = 0, ClsW = 0;  //DO״̬��DOд�Ĵ���

        //ʾ�����ı���
        public static ushort Tbox;
        public static int TboxIOR;  //�ӵ͵��߷ֱ�Ϊ���ֶ�ʹ�ܡ���ʼ����ͣ����ͣ״̬��ģʽ��01ʾ�̣�10Զ�̣�11�طţ�
        public static int TboxIOW;  //�ӵ͵��߷ֱ�Ϊ����ʼ�ƣ���ͣ�ƣ��������ޣ����ŷ�׼����ɾ�������룬�޸ģ�����һ�����ⲿ�ᣬ�������飬ȡ������

        //��Ҫ��־λ
        public static int Mod = 1; //����ģʽ��1ʾ�̣�2Զ�̣�3����
        public static bool Start = false;  //Start��ť״̬
        public static bool Hold = false;  //Hold��ť״̬
        public static bool ServPermit = false;  //����ʾ���ŷ�on�����ο��أ�

        public static bool ServoR = false, oServoR = false, ServoW = false; //��ͣ��״̬������������(���)��oServoR����ָʾǰʱ��״̬��ʶ�������أ�������ͣ��
        public static bool Manu = true; //�ֶ�/�Զ���־
        public static bool StartFace = false;  //StartFace����ʶ������Start
        public static bool EmgStop1 = false, EmgStop2 = false;  //���Ҷ����ļ�ͣ�ź�
        public static bool BrakeOff = false;  //������ɲ���źţ���ֹ�ϵ���硰��ͷ����
        public static Mutex SCAN = new Mutex();  //����ɨ��˿ڵı�־

        //�˿�ӳ�����
        public static bool[] IN = new bool[16];
        public static bool[] OTR = new bool[16];
        public static bool[] OTW = new bool[16];
        public static bool Cls0, Cls1, Cls2, Cls3;

        //ʾ�̺��������ӳ�����
        //�ֶ�ʹ�ܡ���ʼ����ͣ����ͣ״̬��ģʽ��01ʾ�̣�10Զ�̣�11�طţ�
        public static bool TB_deadman = true, TB_start, TB_hold, TB_emergency, TB_teach = false, TB_play = true, TB_remote;
        //��ʼ�ƣ���ͣ�ƣ��������ޣ����ŷ�׼����ɾ�������룬�޸ģ�����һ�����ⲿ�ᣬ�������飬ȡ������
        public static bool TB_green, TB_white, TB_buzzer, TB_servoonready, TB_delete, TB_insert, TB_modify, TB_informlist, TB_extraaxis = false, TB_robotgroup = false, TB_releasestrict;

        //PLC����ʱ������=======================================================================================================

        //ί������
        private delegate void TIMECALLBACK(uint wTimerID, uint msg, uint dwUser, uint dw1, uint dw2);
        //����ί���¼�
        private static event TIMECALLBACK timeCallBack;
        //������
        private static uint mEvent;

        //�����ý�嶨ʱ������  
        [DllImport("\\windows\\mmtimer.dll")]
        private static extern uint timeBeginPeriod(uint uPeriod);
        [DllImport("\\windows\\mmtimer.dll")]
        private static extern uint timeEndPeriod(uint uPeriod);
        [DllImport("\\windows\\mmtimer.dll")]
        private static extern uint timeSetEvent(uint uDelay,
            uint uResolution,
            TIMECALLBACK lpFunction,
            uint dwUser,
            uint uFlags);
        [DllImport("\\windows\\mmtimer.dll")]
        private static extern uint timeKillEvent(uint uTimerID);

        //�����¼�����
        public static void RC_Build()
        {
            timeCallBack += new TIMECALLBACK(SysPLC);
        }

        //����ʱ��  
        public static void RC_Open()
        {
            timeBeginPeriod(1);
            mEvent = timeSetEvent(T_PLC, 1, timeCallBack, 0, 1);  //���ö�ý�嶨ʱ��
        }

        //�ض�ʱ��
        public static void RC_Shut()
        {
            if (mEvent != 0)
            {
                timeEndPeriod(1);
                timeKillEvent(mEvent);
            }
        }

        //˽�г�Ա==============================================================================================================
        //��ý�嶨ʱ���ص�������PLC������ѭ��

        //�˿�ӳ�䵽������
        private static void MapI()
        {
            UserIOR();
            TboxIN();
        }

        //������ӳ�䵽�˿�
        private static void MapO()
        {
            UserIOW();
            TboxOT();
        }

        //PLC��������  
        private static void Process()
        {
            if (IN[0]) EmgStop1 = false;
            else EmgStop1 = true;  //��ͣΪ���գ�����������ʱδ����ͣ

            ServoW = true;
            BrakeOff = false;

            if (ServoW) OTW[3] = true;  //���ŷ�
            else OTW[3] = false;

            if (BrakeOff) OTW[1] = true;  //ɲ��
            else OTW[1] = false;


            //if (TB_emergency) //����ͣ����Ƿѹ����ɲ������ŷ�
            //{
            //    //BrakeOff = false;

            //    //if (BrakeOff) DOW = DOW | 0x0002;
            //    //else DOW = DOW & 0xfffd;

            //    //if (Motion.GTS.WaitOne(5, false))
            //    //{
            //    //    Gts.GT_SetDo(Gts.MC_GPO, ~DOW);  //��ͨDO
            //    //}
            //    //else
            //    //{
            //    //    if (Motion.GTS.WaitOne(5, false))
            //    //        Gts.GT_SetDo(Gts.MC_GPO, ~DOW);  //��ͨDO
            //    //}
            //    //Motion.GTS.ReleaseMutex();  //�ͷŹ̸���Դ

            //    ServoW = true;
            //    short rtn = 1;
            //    //����ֹͣ�˶���
            //    while (true)  //������Դռ�ñ�־��ֱ�ӳ��Թرգ���ʧ��2ms������
            //    {
            //        if (rtn != 0)
            //        {
            //            rtn = Gts.GT_Stop(0xff, 0xff);//��ͣ�˶�
            //        }

            //        if (rtn == 0)
            //        {
            //            Motion.RC_ServoOff(0x3f);
            //            break;
            //        }
            //    }
            //}

            if (TB_teach && !TB_deadman)
            {
                //if (axiSts1+axiSts2+axiSts3+axiSts4!=0)
                if (TB_servoonready)
                    Motion.RC_ServoOff(0x3f);
            }
        }

        //�ص���������TIMECALLBACKί�в���һ��
        private static void SysPLC(uint wTimerID, uint msg, uint dwUser, uint dw1, uint dw2)
        {
            int rtn = 0;
            //�����̸���Դ��5ms���ܻ���򷵻�
            if (!Motion.GTS.WaitOne(5, false)) return;
            //�����Ĵ�����5ms���ܻ���򷵻�
            if (!SCAN.WaitOne(5, false))
            {
                Motion.GTS.ReleaseMutex();
                return;
            }

            //��ȡ����IO�˿ڣ�ע�⣡�̸��Ǹ��߼���0Ϊon��1Ϊoff�����˴�ȡ����Ϊ���߼�
            rtn = Gts.GT_GetDi(Gts.MC_LIMIT_NEGATIVE, out LLimit);//��ȡ����λ
            LLimit = ~LLimit & 0xffff;
            if (rtn != 0)
                LLimit = ~LLimit & 0xffff;

            rtn = Gts.GT_GetDi(Gts.MC_LIMIT_POSITIVE, out RLimit);//��ȡ����λ

            RLimit = ~RLimit & 0xffff;
            if (rtn != 0)
                RLimit = ~RLimit & 0xffff;

            rtn = Gts.GT_GetDi(Gts.MC_ALARM, out Alarm);//��ȡ����������
            Alarm = Alarm & 0xffff;
            if (rtn != 0)
            { }

            rtn = Gts.GT_GetDi(Gts.MC_HOME, out Home);//��ȡԭ��
            Home = ~Home & 0xffff;
            if (rtn != 0)
                Home = ~Home & 0xffff;

            rtn = Gts.GT_GetDi(Gts.MC_GPI, out DI);//��ȡͨ������
            DI = ~DI & 0xffff;
            if (rtn != 0)
                DI = ~DI & 0xffff;

            rtn = Gts.GT_GetDo(Gts.MC_ENABLE, out ServR);//��ȡ�ŷ�ʹ�����
            ServR = ServR & 0xffff;
            if (rtn != 0)
            { }

            rtn = Gts.GT_GetDo(Gts.MC_CLEAR, out ClsR);//��ȡ������λ���
            ClsR = ~ClsR & 0xffff;
            if (rtn != 0)
                ClsR = ~ClsR & 0xffff;

            rtn = Gts.GT_GetDo(Gts.MC_GPO, out DOR);//��ȡͨ�����
            DOR = ~DOR & 0xffff;
            if (rtn != 0)
                DOR = ~DOR & 0xffff;

            rtn = Gts.GT_GetExtIoValue(0, out Tbox, (byte)7);
            //rtn = Gts.GT_GetVal_eHMI((short)0, out Tbox);
            //rtn = Gts.GT_GetSts_eHMI(out Tbox);

            TboxIOR = Tbox;
            SCAN.ReleaseMutex();  //�ͷ�ɨ��Ĵ���
            Motion.GTS.ReleaseMutex();  //�ͷŹ̸���Դ

            //ӳ�䵽������
            //��ȡʾ�̺�IO����
            MapI();

            //�����߼���ϵ
            Process();

            //ӳ�䵽�˿�
            //��ȡʾ�̺�IO���
            MapO();

            //�����̸���Դ��5ms���ܻ���򷵻�
            if (!Motion.GTS.WaitOne(5, false)) return;
            //�����Ĵ�����5ms���ܻ���򷵻�
            if (!SCAN.WaitOne(5, false))
            {
                Motion.GTS.ReleaseMutex();
                return;
            }

            Gts.GT_SetDo(Gts.MC_GPO, ~DOW);  //��ͨDO

            if (Cls0) Gts.GT_ClrSts(1, 1);  //����״̬���
            if (Cls1) Gts.GT_ClrSts(2, 1);
            if (Cls2) Gts.GT_ClrSts(3, 1);
            if (Cls3) Gts.GT_ClrSts(4, 1);

            Gts.GT_SetExtIoValue(0, (ushort)TboxIOW, (byte)7);  //ʾ�̺�ָʾ��

            SCAN.ReleaseMutex();  //�ͷ�ɨ��Ĵ���
            Motion.GTS.ReleaseMutex();  //�ͷŹ̸���Դ
        }


        //ӳ��ʾ�̺�IO����
        private static void TboxIN()
        {
            if ((TboxIOR & 0x01) != 0)  //����ʹ�ܿ���
                TB_deadman = true;
            else
                TB_deadman = false;

            if ((TboxIOR & 0x02) != 0)  //Start��ť
                TB_start = true;
            else
                TB_start = false;

            if ((TboxIOR & 0x04) != 0)  //Hold��ť
                TB_hold = true;
            else
                TB_hold = false;

            if ((TboxIOR & 0x08) != 0)  //ʾ�̺м�ͣ��ť������ ��ͣΪ����
                TB_emergency = false;
            else
                TB_emergency = true;

            if ((TboxIOR & 0x0010) == 0x10 && (TboxIOR & 0x0020) != 0x20)  //������ť1 ʾ��
            {
                TB_remote = false; TB_play = false; TB_teach = true;
            }
            else if ((TboxIOR & 0x0010) == 0x10 && (TboxIOR & 0x0020) == 0x20)  //������ť2 Զ��
            {
                TB_teach = false; TB_remote = false; TB_play = true;
            }
            else if ((TboxIOR & 0x0010) != 0x10 && (TboxIOR & 0x0020) == 0x20)  //������ť3 play
            {
                TB_teach = false; TB_play = false; TB_remote = true;
            }
            TB_deadman = true;

            return;
        }

        //ӳ��ʾ�̺�IO��� TB_green, TB_white, TB_buzzer, TB_servoonready, TB_delete, TB_insert, TB_modify, TB_informlist, TB_extraaxis, TB_robotgroup, TB_releasestrict
        private static void TboxOT()
        {
            if (TB_green)  //���е�
                TboxIOW = TboxIOW | 0x01;  //�������е�
            else
                TboxIOW = TboxIOW & ~0x01;  //�ر����е�;

            if (TB_white)  //��ͣ��
                TboxIOW = TboxIOW | 0x02;  //������ͣ��
            else
                TboxIOW = TboxIOW & ~0x02;  //�ر���ͣ��

            if (TB_servoonready)  //�ŷ�׼����
                TboxIOW = TboxIOW | 0x08;  //�����ŷ�׼����
            else
                TboxIOW = TboxIOW & ~0x08;  //�ر���ͣ��

            if (TB_delete)  //ɾ����
                TboxIOW = TboxIOW | 0x10;  //����ɾ����
            else
                TboxIOW = TboxIOW & ~0x10;  //�ر�ɾ����

            if (TB_insert)  //�����
                TboxIOW = TboxIOW | 0x20;  //���������
            else
                TboxIOW = TboxIOW & ~0x20;  //�رղ����

            if (TB_modify)  //�޸ĵ�
                TboxIOW = TboxIOW | 0x40;  //�����޸ĵ�
            else
                TboxIOW = TboxIOW & ~0x40;  //�ر��޸ĵ�

            if (TB_informlist)  //����һ���� 
                TboxIOW = TboxIOW | 0x80;  //��������һ����
            else
                TboxIOW = TboxIOW & ~0x80;  //�ر�����һ����

            if (TB_extraaxis)  //�ⲿ���
                TboxIOW = TboxIOW | 0x100;  //�����ⲿ���
            else
                TboxIOW = TboxIOW & ~0x100;  //�ر��ⲿ���

            if (TB_robotgroup)  //���������
                TboxIOW = TboxIOW | 0x200;  //�������������
            else
                TboxIOW = TboxIOW & ~0x200;  //�رջ��������

            if (TB_releasestrict)  //ȡ�����Ƶ�
                TboxIOW = TboxIOW | 0x400;  //����ȡ�����Ƶ�
            else
                TboxIOW = TboxIOW & ~0x400;  //�ر�ȡ�����Ƶ�

            return;
        }

        //�˿�ӳ�䵽������
        private static void UserIOR()
        {
            for (int i = 0; i < 16; i++)
            {
                if ((DI & (0x0001 << i)) == 0) IN[i] = false;
                else IN[i] = true;
            }

            for (int i = 0; i < 16; i++)
            {
                if ((DOR & (0x0001 << i)) == 0) OTR[i] = false;
                else OTR[i] = true;
            }
        }

        //������ӳ�䵽�˿�
        private static void UserIOW()
        {
            for (int i = 0; i < 16; i++)
            {
                if (OTW[i]) DOW = DOW | (0x0001 << i);
                else DOW = DOW & ~(0x0001 << i);
            }
        }
    }
}
