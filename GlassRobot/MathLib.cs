using System.Runtime.InteropServices;

namespace GlassRobot
{
    /// <summary>
    /// ���ڷ�װMathFun��������
    /// </summary>
    public class MathFun
    {
        public const string MathPath = @".\MathLibT.dll";  //��ѧ����������·��

        public enum COORDtyp { Joint, Cylinder, Robot, World, Tool1 = 100, Tool2, Tool3, Tool4, User1 = 200, User2, User3, User4 };  //ö���������ͣ�����λ��������������ϵ��Joint�ؽ����꣬Robot�ǻ�����0���꣬�������˶�ѧ�������ꡣWorld�������ꡣ��

        public struct JPOStyp  //�ؽ�����
        {
            public double J1;
            public double J2;
            public double J3;
            public double J4;
            public double J5;
            public double J6;
        }

        public struct CPOStyp  //Բ������
        {
            public double R;
            public double A;
            public double H;
            public double PHI;
            public double THETA;
            public double PSI;
        }

        public struct EPOStyp  //ֱ�����꣨ŷ����
        {
            public COORDtyp C;  //����ϵ
            public double X;
            public double Y;
            public double Z;//3��λ��
            public double PHI;
            public double THETA;
            public double PSI;//3����̬��Z-Y-Zŷ���ǣ�
        }

        public struct QPOStyp //ֱ�����꣨α��Ԫ����������ֱ���жϹ�����̬
        {
            public COORDtyp C;//����ϵ
            public double X;
            public double Y;
            public double Z;//3��λ��
            public double kx;
            public double ky;
            public double kz;//��̬��pos = k*(x,y,z)��(x,y,z)Ϊ����Z��ָ��ĵ�λ������k��ʾ����������Z���ת��Ȧ����k=exp(Angle)
        }

        public struct HCOORDtyp  //ֱ�������任������Σ�
        {
            public COORDtyp C;
            public double X;
            public double Y;
            public double Z;
            public double R11;
            public double R12;
            public double R13;
            public double R21;
            public double R22;
            public double R23;
            public double R31;
            public double R32;
            public double R33;
        }

        /* ��Ҫ���� */
        //���嵱���ĵ�������ĳ�ؽ�ÿȦ������������ֱ�߹ؽ�ÿ���������������������ÿȦ������ x ���������ٱ�
        public static double[] KJ = new double[6];

        //ԭ��ƫ��������������ͣ��������еԭ��ʱ�ı���������/��-KJ����//��ȡ���ؽڽ� = ����/KJ + HJ;  ��������� = (�ؽڽ� - HJ) * KJ;
        public static double[] HJ = new double[6];

        /* ��ѧ���� */
        [DllImport(MathPath)]
        public static extern int MF_COORDTRANS(HCOORDtyp SOURCE, HCOORDtyp TRANS, ref HCOORDtyp RESULT);  //����任

        [DllImport(MathPath)]
        public static extern int MF_CYLINDER2JOINT(CPOStyp CPOS, ref JPOStyp JPOS, int FLAG);  //Բ������ת��Ϊ�ؽ�����

        [DllImport(MathPath)]
        public static extern int MF_EUL2TR(EPOStyp EPOS, ref HCOORDtyp HPOS);  //ŷ������->�������

        [DllImport(MathPath)]
        public static extern int MF_HCOORDINIT(ref HCOORDtyp HCOORD);  //��ʼ��һ����λ��

        [DllImport(MathPath)]
        public static extern int MF_IMOVL(JPOStyp JPOS, EPOStyp DIR, double V, ref JPOStyp JV);  //����ֱ�ߣ��ֶ��ã�

        [DllImport(MathPath)]
        public static extern int MF_ISINTERFERENCE(JPOStyp JPOS, double[] PP, double[] PN);  //����Ƿ��ڸ�������

        [DllImport(MathPath)]
        public static extern int MF_JOINT2CYLINDER(JPOStyp JPOS, ref CPOStyp CPOS);  //�ؽ�����ת��ΪԲ������

        [DllImport(MathPath)]
        public static extern int MF_JOINT2PULSE(JPOStyp JPOS, double[] ENC);  //�ؽ�����ת��Ϊ������

        [DllImport(MathPath)]
        public static extern int MF_JOINT2ROBOT(JPOStyp JPOS, ref HCOORDtyp HPOS);  //�ؽ�����ת��Ϊ����������

        [DllImport(MathPath)]
        public static extern int MF_MOVC(HCOORDtyp HPOS1, HCOORDtyp HPOS2, HCOORDtyp HPOS3, double A, double V, double AF, double W, int MOD, double HEAD, double TAIL);  //�ռ�Բ���岹

        [DllImport(MathPath)]
        public static extern int MF_MOVCW(HCOORDtyp HPOS1, HCOORDtyp HPOS2, HCOORDtyp HPOS3, double A, double V, double AF, double W, int MOD, double HEAD, double TAIL);  //�ռ�Բ���岹

        [DllImport(MathPath)]
        public static extern int MF_MOVJ(JPOStyp JPOS1, JPOStyp JPOS2, JPOStyp JVO, double V, double TAIL, int PT);  //��λ�˶��岹

        [DllImport(MathPath)]
        public static extern int MF_MOVJS(JPOStyp[] JPOS, JPOStyp JV, double[] V, int NUM, int PT, int FIN);  //����PTP�˶����ؽڿռ��ϵ�������

        [DllImport(MathPath)]
        public static extern int MF_MOVL(HCOORDtyp HPOS1, HCOORDtyp HPOS2, double A, double V, double AF, double W, int MOD, double HEADLEN, double TAILLEN);  //�ռ�ֱ�߲岹

        [DllImport(MathPath)]
        public static extern int MF_MOVS(HCOORDtyp[] HPOS, double[] V, double A, int NUM, int FIN);  //�������߲岹

        [DllImport(MathPath)]
        public static extern int MF_PULSE2JOINT(double[] ENC, ref JPOStyp JPOS);  //������ת�ؽ�����

        [DllImport(MathPath)]
        public static extern int MF_QP2TR(QPOStyp QPOS, ref HCOORDtyp HPOS);  //α��Ԫ����ת�������

        [DllImport(MathPath)]
        public static extern int MF_ROBOT2JOINT(HCOORDtyp HPOS, ref JPOStyp JPOS, int FLAG);  //����������ת��Ϊ�ؽ�����

        [DllImport(MathPath)]
        public static extern int MF_SETTOOLCOORD(JPOStyp[] JPOS, ref HCOORDtyp TOOLCOORD);  //3�㽨����������ϵ

        [DllImport(MathPath)]
        public static extern int MF_SETUSERCOORD(JPOStyp[] JPOS, ref HCOORDtyp USERCOORD);  //3��ȷ���û�����ϵ

        [DllImport(MathPath)]
        public static extern int MF_TOOL2WRIST(HCOORDtyp TOOL, HCOORDtyp TRANS, ref HCOORDtyp WRIST);  //��֪���ߵ��������������

        [DllImport(MathPath)]
        public static extern int MF_TR2EUL(HCOORDtyp HCOORD, ref EPOStyp EPOS, int FLAG);  //�������->ŷ������

        [DllImport(MathPath)]
        public static extern int MF_TR2QP(HCOORDtyp HPOS, ref QPOStyp QPOS);  //�������תα��Ԫ����

        [DllImport(MathPath)]
        public static extern int MF_WRIST2TOOL(HCOORDtyp WRIST, HCOORDtyp TRANS, ref HCOORDtyp TOOL);  //��֪��������󹤾ߵ�����

        /* �趨�Ͷ�ȡ�ؼ����� */
        [DllImport(MathPath)]
        public static extern int MF_INITMATHFUN(double[] LIMITP, double[] LIMITN, HCOORDtyp WORLD, HCOORDtyp USER, HCOORDtyp TOOL, double[] JV, double[] JA, double V, double W, double A, double AF, double[] HOME, double DT);  //��ʼ��
        [DllImport(MathPath)]
        public static extern int MF_INITPOS(JPOStyp CURPOS);  //����ǰλ����Ϊ��ʼλ�ã������϶ι滮���ࣩ

        [DllImport(MathPath)]
        public static extern int MF_SETLIMIT(double[] LIMITP, double[] LIMITN);  //�趨����λ
        [DllImport(MathPath)]
        public static extern int MF_GETLIMIT(double[] LIMITP, double[] LIMITN);  //��ȡ����λ

        [DllImport(MathPath)]
        public static extern int MF_SETWORLD(HCOORDtyp WORLD);  //�趨��������
        [DllImport(MathPath)]
        public static extern int MF_GETWORLD(ref HCOORDtyp WORLD);  //��ȡ��������

        [DllImport(MathPath)]
        public static extern int MF_SETUSER(HCOORDtyp USER);  //�趨�û�����
        [DllImport(MathPath)]
        public static extern int MF_GETUSER(ref HCOORDtyp USER);  //��ȡ�û�����

        [DllImport(MathPath)]
        public static extern int MF_SETTOOL(HCOORDtyp TOOL);  //�趨��������
        [DllImport(MathPath)]
        public static extern int MF_GETTOOL(ref HCOORDtyp TOOL);  //��ȡ��������

        [DllImport(MathPath)]
        public static extern int MF_SETVMAX(double[] JV);  //�趨����ٶȣ��ؽڣ�
        [DllImport(MathPath)]
        public static extern int MF_GETVMAX(double[] JV);  //��ȡ����ٶȣ��ؽڣ�

        [DllImport(MathPath)]
        public static extern int MF_SETVMAXL(double V, double W);  //�趨����ٶȣ�CP��
        [DllImport(MathPath)]
        public static extern int MF_GETVMAXL(ref double V, ref double W);  //��ȡ����ٶȣ�CP��

        [DllImport(MathPath)]
        public static extern int MF_SETAMAX(double[] JA);  //�趨��߼��ٶȣ��ؽڣ�
        [DllImport(MathPath)]
        public static extern int MF_GETAMAX(double[] JA);  //��ȡ��߼��ٶȣ��ؽڣ�

        [DllImport(MathPath)]
        public static extern int MF_SETAMAXL(double A, double AF);  //�趨��߼��ٶȣ�CP��
        [DllImport(MathPath)]
        public static extern int MF_GETAMAXL(ref double A, ref double AF);  //��ȡ��߼��ٶȣ�CP��

        [DllImport(MathPath)]
        public static extern int MF_SETHOME(double[] ENC);  //�趨��еԭ��
        [DllImport(MathPath)]
        public static extern int MF_GETHOME(double[] ENC);  //��ȡ��еԭ��

        [DllImport(MathPath)]
        public static extern int MF_SETTIMER(double DT);  //�趨�岹����
        [DllImport(MathPath)]
        public static extern int MF_GETTIMER(ref double DT);  //��ȡ�岹����

        [DllImport(MathPath)]
        public static extern int MF_SETCURJPOS(JPOStyp CURJPOS);  //�趨��ǰλ�ã����ڷ�����ѡ���ȣ�
        [DllImport(MathPath)]
        public static extern int MF_GETREMAIN(ref JPOStyp JPOS, ref JPOStyp JV, ref HCOORDtyp HPOS, double[] HV);  //��ȡ�����λ�ú��ٶȣ������������ɶΣ�

        /* ��ȡ�岹���� */
        [DllImport(MathPath)]
        public static extern int MF_GETPVT(ref double PTAB, ref double VTAB, ref double TTAB, ref int NUM, int FIN, int I);  //��ȡPVT��

        [DllImport(MathPath)]
        public static extern int MF_GETPT(ref double JPT1, ref double JPT2, ref double JPT3, ref double JPT4, ref double JPT5, ref double JPT6, ref int STEP);  //��ȡPT��

    }
}
