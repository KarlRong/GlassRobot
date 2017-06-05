using System;
using System.Runtime.InteropServices;

namespace Controller
{
    /// <summary>
    /// ���ڷ�װMathFun��������
    /// </summary>
    public class MathFun
    {
        public enum COORDtyp { Joint, Robot, World };  //ö���������ͣ�����λ��������������ϵ��Joint�ؽ����꣬Robot�ǻ��������꣬�������˶�ѧ�������ꡣWorld�������ꡣ��

        public struct JPOStyp  //�ؽ�����
        {
            public double J1;
            public double J2;
            public double J3;
            public double J4;

            //���ء�*���������������ţ�
            public static JPOStyp operator *(double k, JPOStyp JPos)
            {
                JPOStyp temp = new JPOStyp();
                temp.J1 = k * JPos.J1;
                temp.J2 = k * JPos.J2;
                temp.J3 = k * JPos.J3;
                temp.J4 = k * JPos.J4;

                return temp;
            }

            //���ء�-���������������ţ�
            public static JPOStyp operator -(JPOStyp JPos1, JPOStyp JPos2)
            {
                JPOStyp temp = new JPOStyp();
                temp.J1 = JPos1.J1 - JPos2.J1;
                temp.J2 = JPos1.J2 - JPos2.J2;
                temp.J3 = JPos1.J3 - JPos2.J3;
                temp.J4 = JPos1.J4 - JPos2.J4;

                return temp;
            }

            public static double[] JPOS2Array(JPOStyp JPOS)
            {
                double[] J = new double[4];

                J[0] = JPOS.J1;
                J[1] = JPOS.J2;
                J[2] = JPOS.J3;
                J[3] = JPOS.J4;
                return J;
            }
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
            
            //���ء�*���������������ţ�
            public static EPOStyp operator *(double k, EPOStyp EPos)
            {
                EPOStyp temp = new EPOStyp();
                temp.C = EPos.C;
                temp.X = k * EPos.X;
                temp.Y = k * EPos.Y;
                temp.Z = k * EPos.Z;
                temp.PHI = k * EPos.PHI;
                temp.THETA = k * EPos.THETA;
                temp.PSI = k * EPos.PSI;

                return temp;
            }
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

            //���ء�*����������˷���
            public static HCOORDtyp operator *(HCOORDtyp firHCoord, HCOORDtyp secHCoord)
            {
                HCOORDtyp temp = new HCOORDtyp();
                temp.C = firHCoord.C;
                temp.R11 = firHCoord.R11 * secHCoord.R11 + firHCoord.R12 * secHCoord.R21 + firHCoord.R13 * secHCoord.R31;
                temp.R12 = firHCoord.R11 * secHCoord.R12 + firHCoord.R12 * secHCoord.R22 + firHCoord.R13 * secHCoord.R32;
                temp.R13 = firHCoord.R11 * secHCoord.R13 + firHCoord.R12 * secHCoord.R23 + firHCoord.R13 * secHCoord.R33;

                temp.R21 = firHCoord.R21 * secHCoord.R11 + firHCoord.R22 * secHCoord.R21 + firHCoord.R23 * secHCoord.R31;
                temp.R22 = firHCoord.R21 * secHCoord.R12 + firHCoord.R22 * secHCoord.R22 + firHCoord.R23 * secHCoord.R32;
                temp.R23 = firHCoord.R21 * secHCoord.R13 + firHCoord.R22 * secHCoord.R23 + firHCoord.R23 * secHCoord.R33;

                temp.R31 = firHCoord.R31 * secHCoord.R11 + firHCoord.R32 * secHCoord.R21 + firHCoord.R33 * secHCoord.R31;
                temp.R32 = firHCoord.R31 * secHCoord.R12 + firHCoord.R32 * secHCoord.R22 + firHCoord.R33 * secHCoord.R32;
                temp.R33 = firHCoord.R31 * secHCoord.R13 + firHCoord.R32 * secHCoord.R23 + firHCoord.R33 * secHCoord.R33;

                temp.X = firHCoord.R11 * secHCoord.X + firHCoord.R12 * secHCoord.Y + firHCoord.R13 * secHCoord.Z + firHCoord.X;
                temp.Y = firHCoord.R21 * secHCoord.X + firHCoord.R22 * secHCoord.Y + firHCoord.R23 * secHCoord.Z + firHCoord.Y;
                temp.Z = firHCoord.R31 * secHCoord.X + firHCoord.R32 * secHCoord.Y + firHCoord.R33 * secHCoord.Z + firHCoord.Z;

                return temp;
            }

            //�����������
            public static HCOORDtyp inv(HCOORDtyp HCoord)
            {
                //��������������������ԣ�����������Բ��ø��򵥵ķ�ʽ
                //A(1,2,3;1,2,3)���ɵ�λ�������󣬶�A����ĵ�4��Ϊ[0 0 0 1]���Կ����ñȽϼ򵥵ķ�ʽ���档
                HCOORDtyp temp = new HCOORDtyp();
                temp.C = HCoord.C;

                //��ת�����ǵ�λ������ת�ü�����
                temp.R11 = HCoord.R11; temp.R12 = HCoord.R21; temp.R13 = HCoord.R31;
                temp.R21 = HCoord.R12; temp.R22 = HCoord.R22; temp.R23 = HCoord.R32;
                temp.R31 = HCoord.R13; temp.R32 = HCoord.R23; temp.R33 = HCoord.R33;

                //ƽ�Ʒ���
                temp.X = -(HCoord.R11 * HCoord.X + HCoord.R21 * HCoord.Y + HCoord.R31 * HCoord.Z);
                temp.Y = -(HCoord.R12 * HCoord.X + HCoord.R22 * HCoord.Y + HCoord.R32 * HCoord.Z);
                temp.Z = -(HCoord.R13 * HCoord.X + HCoord.R23 * HCoord.Y + HCoord.R33 * HCoord.Z);

                return temp;
            }
        }

        /* ��Ҫ���� */
        //���嵱���ĵ�������ĳ�ؽ�ÿ����������������ֱ�߹ؽ�ÿ���������������������ÿ���������� x ���������ٱ�
        public static double[] KJ = { 5000 * 159 / Math.PI, -5000 * 158 / Math.PI, 5000 * 141 / Math.PI, 5000 * 141 / Math.PI, 5000 * 159 / Math.PI, -5000 * 158 / Math.PI, 5000 * 141 / Math.PI, 5000 * 141 / Math.PI };
        
        //ԭ��ƫ��������������ͣ��������еԭ��ʱ�ı���������/��-KJ����//��ȡ���ؽڽ� = ����/KJ + HJ;  ��������� = (�ؽڽ� - HJ) * KJ;
        public static double[] HJ = { Math.PI / 2, 0, 0, 0, 0, 0, 0, 0 };

        //��������任����
        public static HCOORDtyp TOOL = new HCOORDtyp();

        //���ؽ����ת�٣����ٺ�
        private static double[] Vmax = { 1, 1, 1, 1 };  //���ת�٣�����/s

        //���ؽ������ٶ�
        private static double[] Amax = { 1, 1, 1, 1 };

        //��е�ṹ����
        private static double[] a = { 1.35, 0.55, 0, 0, 0, 0 };
        private static double[] alf = { 0, 0, Math.PI / 2, 0, 0, 0 };
        private static double[] d = { 0, 0, 0, 0.27, 0, 0 };
        
        //�ο�����ٶ�
        public static JPOStyp curJPOS = new JPOStyp();  //���ڷ���Ĳο�λ�ã�һ�����ϴη���ֵ�����ߵ�ǰֵ���ⲿͨ���������ã�
        public static JPOStyp JVF = new JPOStyp();  //�ϴι滮��ĩ���ٶ�
        public static JPOStyp JPF = new JPOStyp();  //�ϴι滮��ĩ��λ��

        //PVT�˶����ݱ����ű����岹����1024������������Ҫ����ʹ�ã��������ڴ���MOVJS�������
        //��ģ���������PVT����ȴ��˶��߳̽����ȡ��գ�Ȼ��Ŵӿ�ͷ��������д
        public static double[,] Ptab = new double[4, 1024];
        public static double[,] Ttab = new double[4, 1024];
        public static int PVTbusy = 0;  //PVTռ�ñ�־��=1���������̶߳�д��
        public static int PVTready = 0;  //PVT�����
        public static int PVTIndex = 0;  //PVT������
        
        /* ��ѧ���� */
        
        //���������y����ת
        private static HCOORDtyp roty(HCOORDtyp POS, double Ang)
        {
            //POSԭλ�ˣ�Ang��ת�Ƕ�
            HCOORDtyp trans;
            trans.C = POS.C;
            trans.R11 = Math.Cos(Ang);
            trans.R12 = 0;
            trans.R13 = -Math.Sin(Ang);
            trans.R21 = 0;
            trans.R22 = 1;
            trans.R23 = 0;
            trans.R31 = Math.Sin(Ang);
            trans.R32 = 0;
            trans.R33 = Math.Cos(Ang);
            trans.X = 0;
            trans.Y = 0;
            trans.Z = 0;

            return (POS * trans);
        }

        //��֪���˵��ٶȣ����룬����ٶȼ��ٶȣ���׼��5�������ٶȹ滮 //�յ㣿����
        private static int PLANV(double S, double Vo, double Vf, double A, double V, ref double[] t, ref double[] s)
        {
            //S���룬Vo/Vf�˵��ٶȣ�V����ٶȡ�A���ٶȡ�t[0]���ٶ�ʱ�䡢t[1]���ٶ�ʱ�䡢t[2]���ٶ�ʱ��
            //������ظ����ٶȡ����ٶ������µ��˶�ʱ���λ��

            double S1, S3, Sf, Vr, Tf, t1, t2, t3;

            Tf = Math.Abs((Vf - Vo) / A);  //���ٶȾ�������С�˶�ʱ�䣨ֱ���������ٶȣ�������յ��ʱ�䣩
            Sf = (Vo + Vf) * Tf / 2;  //��Ӧλ��

            if (S < Sf)  //�յ���Vo��Vf�·�
            {
                V = -V; A = -A;
            }

            t1 = (V - Vo) / A;  //����ʱ��
            t3 = (V - Vf) / A;  //����ʱ��
            S1 = (V + Vo) * t1 / 2;  //���ٶ�λ��
            S3 = (V + Vf) * t3 / 2;  //���ٶ�λ��

            if (((S < (S1 + S3)) && (V >= 0)) || ((S > (S1 + S3)) && (V < 0)))   //����̫�̣����ڼ��ٲ������⣬�������ٶ�
            {
                Vr = Math.Sqrt(A * S + Vo * Vo / 2 + Vf * Vf / 2);  //ʵ���м��ٶ�
                if ((V > 0) && (Vo < 0) && (Vf < 0) && (S < 0) && (Vr < -Vo) && (Vr < -Vf))  //Vo,Vf,S��С���㣬��V������������Vr����Ϊ��
                    Vr = -Vr;
                else if ((V < 0) && (Vo > 0) && (Vf > 0) && (S > 0) && (Vr < Vo) && (Vr < Vf))  //Vo,Vf,S�������㣬��VС����������Vr����Ϊ��
                    Vr = Vr * 1;
                else  //VrȡVͬ����
                    Vr = Math.Sign(V) * Vr;

                t1 = Math.Abs((Vr - Vo) / A);
                t3 = Math.Abs((Vr - Vf) / A);
                t2 = 0;
                S1 = Vo * t1 + (Vr - Vo) * t1 / 2;
                S3 = Vf * t2 + (Vr - Vf) * t3 / 2;
            }
            else  //�����ٶ�
            {
                Vr = V;
                t2 = Math.Abs((S - S1 - S3) / V);

            }
            t[0] = t1; t[1] = t2; t[2] = t3;
            s[0] = S1; s[1] = S - S1 - S3; s[2] = S3;

            return 0;
        }

        //��ʼ��һ����λ��
        public static HCOORDtyp MF_HCOORDINIT()
        {
            HCOORDtyp HCOORD = new HCOORDtyp();
            HCOORD.C = COORDtyp.Robot;
            HCOORD.X = 0;
            HCOORD.Y = 0;
            HCOORD.Z = 0;
            HCOORD.R11 = 1;
            HCOORD.R12 = 0;
            HCOORD.R13 = 0;
            HCOORD.R21 = 0;
            HCOORD.R22 = 1;
            HCOORD.R23 = 0;
            HCOORD.R31 = 0;
            HCOORD.R32 = 0;
            HCOORD.R33 = 1;
            return HCOORD;
        }

        //�ؽ�����ת��Ϊ����������
        public static HCOORDtyp MF_JOINT2ROBOT(JPOStyp JPOS)
        {
            //�˶�ѧ���⣬�ؽ�����->���ߵ�
            HCOORDtyp HPOS = new HCOORDtyp();
            HPOS.C = COORDtyp.Robot;
            HPOS.X = Math.Cos(JPOS.J1) * a[0] + Math.Cos(JPOS.J1 + JPOS.J2) * a[1] + Math.Sin(JPOS.J1 + JPOS.J2 + JPOS.J3) * d[3];
            HPOS.Y = 0;
            HPOS.Z = Math.Sin(JPOS.J1) * a[0] + Math.Sin(JPOS.J1 + JPOS.J2) * a[1] - Math.Cos(JPOS.J1 + JPOS.J2 + JPOS.J3) * d[3];
            HPOS.R11 = 0.5 * Math.Cos(JPOS.J1 + JPOS.J2 + JPOS.J3 - JPOS.J4) + 0.5 * Math.Cos(JPOS.J1 + JPOS.J2 + JPOS.J3 + JPOS.J4);
            HPOS.R12 = -0.5 * Math.Sin(JPOS.J1 + JPOS.J2 + JPOS.J3 + JPOS.J4) + 0.5 * Math.Sin(JPOS.J1 + JPOS.J2 + JPOS.J3 - JPOS.J4);
            HPOS.R13 = Math.Sin(JPOS.J1 + JPOS.J2 + JPOS.J3);
            HPOS.R21 = -Math.Sin(JPOS.J4);
            HPOS.R22 = -Math.Cos(JPOS.J4);
            HPOS.R23 = 0;
            HPOS.R31 = 0.5 * Math.Sin(JPOS.J1 + JPOS.J2 + JPOS.J3 + JPOS.J4) + 0.5 * Math.Sin(JPOS.J1 + JPOS.J2 + JPOS.J3 - JPOS.J4);
            HPOS.R32 = -0.5 * Math.Cos(JPOS.J1 + JPOS.J2 + JPOS.J3 - JPOS.J4) + 0.5 * Math.Cos(JPOS.J1 + JPOS.J2 + JPOS.J3 + JPOS.J4);
            HPOS.R33 = -Math.Cos(JPOS.J1 + JPOS.J2 + JPOS.J3);

            HPOS = HPOS * TOOL;  //�任���ߵ�����

            return HPOS;
        }

        //����������ת��Ϊ�ؽ����� //���
        public static int MF_ROBOT2JOINT(HCOORDtyp HPOS, ref JPOStyp JPOS, int FLAG)
        {
            //�˶�ѧ���⣬���ߵ�->�ؽ�����
            //HPOS������꣬JPOS�ؽ����꣬FLAG��Ӧ����־��bit0=1��Ӧ1�ؽڲ���overhead��̬��bit1=1��Ӧ3�ؽڵ����ͣ�bit2=1��Ӧ5�ؽ�>0���������bit3=1ȷ��ǿ���趨�����̬��������ݵ�ǰ��̬�Զ��趨��

            double q1, q2, q3, q4, q123, x3, z3;

            HPOS = HPOS * HCOORDtyp.inv(TOOL);  //�任�������

            q4 = Math.Atan2(-HPOS.R21, -HPOS.R22);
            q123 = Math.Atan2(HPOS.R31, -HPOS.R33);
            x3 = HPOS.X - Math.Sin(q123) * d[3];
            z3 = HPOS.Z + Math.Cos(q123) * d[3];
            q2 = Math.Acos((x3 * x3 + z3 * z3 - a[0] * a[0] - a[1] * a[1]) / (2.0 * a[0] * a[1]));  //���Ҷ���
            if ((FLAG & 0x00000008) != 0)  //ָ������
            {
                if ((FLAG & 0x00000002) == 0)  //������,q2<0
                    q2 = -q2;
            }
            else  //��ָ��
            {
                if (curJPOS.J2 < 0)  //���ϴμ���ȷ������
                    q2 = -q2;
            }

            q1 = -Math.Atan2((-z3 * a[1] * Math.Cos(q2) + x3 * a[1] * Math.Sin(q2) - z3 * a[0]), (z3 * a[1] * Math.Sin(q2) + x3 * a[1] * Math.Cos(q2) + x3 * a[0]));
            q3 = q123 - q2 - q1;

            JPOS.J1 = q1; JPOS.J2 = q2; JPOS.J3 = q3; JPOS.J4 = q4;

            //���������Ϊ��ǰ�㣬��Ϊ�´ζ��ʱ��ѡ�����ݣ���ǰ������û���ʱ�趨
            curJPOS.J1 = JPOS.J1; curJPOS.J2 = JPOS.J2; curJPOS.J3 = JPOS.J3; curJPOS.J4 = JPOS.J4;

            return 0;
        }

        //����ֱ�ߣ��ֶ��ã�
        public static int MF_IMOVL(JPOStyp JPOS, EPOStyp DIR, double V, ref JPOStyp JV)
        {
            //���ص�ǰ˲ʱ���ؽ��ٶȷ��������˶��߳�JOG��ʽʹ��
            //JPOSΪ��ǰ�㣬DIRΪ����������/��ת����VΪ���ٶȻ���ٶȣ�JVָ�����ؽ��ٶ�

            HCOORDtyp hpos;
            JPOStyp dP, P2 = new JPOStyp();

            hpos = MF_JOINT2ROBOT(JPOS);  //�����

            hpos.X = hpos.X + DIR.X / 1000; //ָ��ǰ��1mm����ע�ⳤ�ȵ�λ��m��
            hpos.Y = hpos.Y + DIR.Y / 1000;
            hpos.Z = hpos.Z + DIR.Z / 1000;
            hpos = roty(hpos, DIR.THETA / 1000);  //ָ��ǰ��0.001���ȴ����˴�����ŷ���Ǵ������ݣ���������ŷ���ǣ����ݱ�ʾ�ֱ�������ϵxyz���ת��

            if(MF_ROBOT2JOINT(hpos, ref P2, 0) != 0)  
                return -1;  //���ⲻ����

            dP = P2 - JPOS;
            JV = (V * 1000) * dP;  //�ؽ���������/ʱ�� = ���ؽ��ٶȣ���λrad/s

            return 0;
        }

        //��λ�˶��岹 //��������
        public static int MF_MOVJ(JPOStyp JPOS1, JPOStyp JPOS2, JPOStyp JVO, double V, double TAIL)
        {
            //ͨ�õĵ���PTP�˶��滮��5�η������ٶ�����
            //���ٶ�ָ����ǰ��ʣ�ࣩ��������ָ�������滮��ʣ��в0-100%����ʱ���㣩������ʼĩ���ٶ�Ϊ��
            //����������PTP���ƣ�������ٶ�Ϊ�㣬TAILΪ1������Ϊ����PTP�˶�
            //ȫ��������ٶȺͼ��ٶȼ������ʱ�䣨1���ٶ����ߣ���ȡ������ߣ����������ٶ�V��%���Ŵ��ٰ�������TAIL��%����ȡ
            //Ȼ����ᰴ��ʱ������ʱ���˶��滮
            //���ս��ΪPVT��
            //JPOS1��㣬JPOS2�յ㣬JVO���ٶȣ�V�ٶȣ�%����TAIL����ȣ�%��
            int i, rtn;
            double Tl, Tcut, TK, Vr, Scut;
            double[] SL, jP1, jP2, jVo, jPf = new double[4], jVf = new double[4], t = new double[3], s = new double[3];
            double[,] PS = new double[4, 3];  //����λ�ƶ�
            double[,] PT = new double[4, 3];  //����ʱ���

            if ((V > 1) || (V <= 0) || (TAIL >= 1) || (TAIL < 0))  //���ݲ�����
                return -1;

            //�ؽڽǺ͹ؽ��ٶȶ�תΪ����
            SL = JPOStyp.JPOS2Array(JPOS2 - JPOS1);  //λ��
            jP1 = JPOStyp.JPOS2Array(JPOS1);
            jP2 = JPOStyp.JPOS2Array(JPOS2);
            jVo = JPOStyp.JPOS2Array(JVO);

            if ((Math.Abs(SL[0]) < 0.000001) && (Math.Abs(SL[1]) < 0.000001) && (Math.Abs(SL[2]) < 0.000001) && (Math.Abs(SL[3]) < 0.000001))
                return -1;  //λ�Ʋ���ȫΪ��

            if ((Math.Abs(jVo[0]) > Vmax[0]) || (Math.Abs(jVo[1]) > Vmax[1]) || (Math.Abs(jVo[2]) > Vmax[2]) || (Math.Abs(jVo[3]) > Vmax[3]))
                return -1;  //���ٶȲ��ɴ�������ٶ�

            Vr = 0;  //ʵ������ٶ�
            Tl = 0;  //ʱ�����ֵ


            //����1���ٶ����ߵ���������˶�ʱ�䣨Ϊ����һ����2�ף�����Amax����ʹ�ã�
            for (i = 0; i < 4; i++)
            {
                PLANV(SL[i], jVo[i], 0, Amax[i] / 2, Vmax[i], ref t, ref s);
                PS[i, 0] = s[0]; PS[i, 1] = s[1]; PS[i, 2] = s[2];
                PT[i, 0] = t[0]; PT[i, 1] = t[1]; PT[i, 2] = t[2];
                if (Tl < (t[0] + t[1] + t[2]))  //ȡ�����ʱ��
                    Tl = t[0] + t[1] + t[2];
            }

            //�������ٶȣ�%���Ŵ�ʱ��
            Tl = Tl / V;
            Tcut = Tl * (1 - TAIL);  //�ض�ʱ��
            for (i = 0; i < 4; i++)
            {
                TK = (PT[i, 0] + PT[i, 1] + PT[i, 2]) / Tl;
                if (TK >= 0.000001)
                {
                    PT[i, 0] /= TK;
                    PT[i, 1] /= TK;
                    PT[i, 2] /= TK;
                }
                else
                {
                    PT[i, 0] = Tl / 2;
                    PT[i, 1] = 0;
                    PT[i, 2] = Tl / 2;
                }
            }

            //����ͳһʱ�����¹滮1���ٶ�
            for (i = 0; i < 4; i++)
            {
                if (Tcut < PT[i, 0])  //���ٶξͽضϣ�ֻ��1��
                {
                    Scut = jVo[i] * Tcut + (PS[i, 0] - jVo[i] * PT[i, 0]) * Tcut * Tcut / PT[i, 0] / PT[i, 0];
                    jVf[i] = 2 * Scut / Tcut - jVo[i];
                    PS[i, 0] = jP1[i] + Scut; PT[i, 0] = Tcut;
                    PS[i, 1] = PS[i, 0]; PT[i, 1] = Tcut;
                    PS[i, 2] = PS[i, 0]; PT[i, 2] = Tcut;
                }
                else if (Tcut >= (PT[i, 0] + PT[i, 1]))  //���ٶνضϣ����߲��ضϣ�,��3��
                {
                    Scut = PS[i, 0] + PS[i, 1] + PS[i, 2] * (1 - (Tl - Tcut) * (Tl - Tcut) / PT[i, 2] / PT[i, 2]);
                    if (TAIL > 0.000001)
                        jVf[i] = 2 * (PS[i, 0] + PS[i, 1] + PS[i, 2] - Scut) / (Tl - Tcut);
                    else
                        jVf[i] = 0;
                    PS[i, 0] += jP1[i];
                    PS[i, 1] += PS[i, 0]; PT[i, 1] += PT[i, 0];
                    PS[i, 2] = jP1[i] + Scut; PT[i, 2] = Tcut;
                }
                else  //���ٶνض�, ��2��
                {
                    Scut = PS[i, 0] + PS[i, 1] * (Tcut - PT[i, 0]) / PT[i, 1];
                    jVf[i] = PS[i, 1] / PT[i, 1];
                    PS[i, 0] += jP1[i];
                    PS[i, 1] = PS[i, 0] + Scut; PT[i, 1] = Tcut;
                    PS[i, 2] = PS[i, 1]; PT[i, 2] = Tcut;
                }
            }

            //д��PVT��MOVJ����д��PVTͷ����Ȼ���ȡ��գ�ֻ��MOVJS�Ż��������PVT
            PVTbusy = 1;  //����PVT��
            PVTready = 0;
            for (i = 0; i < 4; i++)
            {
                //д����ʼ��
                Ptab[i, PVTIndex] = jP1[i]; Ttab[i, PVTIndex] += 0;
                //д��1�ֶ�
                Ptab[i, PVTIndex + 1] = PS[i, 0]; Ttab[i, PVTIndex + 1] = Ttab[i, PVTIndex] + PT[i, 0];
                //д��2�ֶ�
                Ptab[i, PVTIndex + 2] = PS[i, 1]; Ttab[i, PVTIndex + 2] = Ttab[i, PVTIndex] + PT[i, 1];
                //д��3�ֶ�
                Ptab[i, PVTIndex + 3] = PS[i, 2]; Ttab[i, PVTIndex + 3] = Ttab[i, PVTIndex] + PT[i, 2];
            }
            PVTIndex += 3;
            PVTbusy = 0;

            JVF.J1 = jVf[0]; JVF.J2 = jVf[1]; JVF.J3 = jVf[2]; JVF.J4 = jVf[3];
            JPF.J1 = PS[0, 2]; JPF.J2 = PS[1, 2]; JPF.J3 = PS[2, 2]; JPF.J4 = PS[3, 2];

            if (TAIL == 0)
                PVTready = 1;  //������ϣ���PTģʽ��PVT�ɶ�

            return 0;
        }

        //��ʼ��
        public static int MF_INITMATHFUN(HCOORDtyp inTOOL, double[] JV, double[] JA, double[] HOME)  //��ʼ��
        {
            TOOL = inTOOL;

            for (int i = 0; i < 4; i++)
            {
                Vmax[i] = JV[i];
                Amax[i] = JA[i];
                HJ[i] = HOME[i];
            }
            
            return 0;
        }

        //�����i��PVT����
        public static int MF_SORTPVT(int i)
        {
            //���㷨�滮��������ʱ��Ϊ��εģ���Ӧ���ٶ�û�е������Ϊ�˱�Ŷ��룬û��ȥ���ظ��㣩�����̸߿������ǲ������
            //���д�������PVT�Ļ�Ҫ���ظ���ȥ��

            int j, k;

            if (PVTready == 0)  //����δ����
                return -7;

            PVTbusy = 1;  //����

            k = 0;
            for (j = 0; j <= PVTIndex; j++)
            {
                if ((j == 0) || ((Ttab[i,j] - Ttab[i,j - 1]) > 0.0001))  //���������һ���ظ���д�루���6��PVT�����ǲ�һ�µģ�
                {
                    Ptab[i,k] = Ptab[i,j];
                    Ttab[i,k] = Ttab[i,j];
                    k++;
                }
            }

            PVTbusy = 0;

            return k;
        }
    }
}
