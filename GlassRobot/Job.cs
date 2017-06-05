using System.IO;
using System;
using System.Collections;
using Controller;

namespace GlassRobot
{
    public class Job
    {
        public string JobName = null;  //�����ļ���

        public ArrayList headLines = new ArrayList();  //���嶯̬���鱣���ļ�ͷ
        public ArrayList textLines = new ArrayList();  //���嶯̬���鱣������У�����ʾ���֣�
        public int lengthHead = 0;  //��¼�ļ�ͷ����
        public int lengthProg = 0;  //��¼����������
        
        public ArrayList poses = new ArrayList();  //���嶯̬���鱣�����ݵ�
        public int NumPos = 0;  //���ݵ����
        public int IndexPos = -1;  //��ǰ���ݵ�����
        
        public int NumUser = 1, NumTool = 1;  //�û�����͹��߱��
        public string name = ""; //�ļ�����
        public string posType;  //λ������
        public string robotGroup1;

        /// <summary>
        /// ��ȡ�����ļ� �ļ�������
        /// </summary>
        public void loadJBI(string fileName)
        {
            StreamReader sr = File.OpenText(fileName);
            JobName = fileName;
            string nextLine;  //������

            headLines.Clear();
            textLines.Clear();
            lengthHead = 0;  //��¼�ļ�ͷ����
            lengthProg = 0;  //��¼����������
            NumPos = 0;  //���ݵ����
            IndexPos = 0;  //��ǰ���ݵ�����
            NumUser = 1;  //�û�����͹��߱��
            NumTool = 1;
            posType = "Joint";  //λ������

            while ((nextLine = sr.ReadLine()) != null)  //�ȶ��ļ�ͷ
            {
                string[] subCMD = nextLine.Split(' ');
                if (subCMD[0] == @"//NAME")  //��ȡλ��������
                {
                    name = subCMD[1];
                }
                else if (subCMD[0] == @"///NPOS")  //��ȡλ��������
                {
                    NumPos = int.Parse(subCMD[1].Substring(0,subCMD[1].IndexOf(",")));
                }
                else if (subCMD[0] == @"///USER")  //��ȡ�����ļ���������
                {
                    NumUser = int.Parse(subCMD[1]);
                }
                else if (subCMD[0] == @"///TOOL")  //��ȡ��������
                {
                    NumTool = int.Parse(subCMD[1]);
                }
                else if (subCMD[0] == @"///POSTYPE")  //��ȡλ������
                {
                    posType = subCMD[1];
                }
                else if (subCMD[0] == @"///GROUP1")//��ȡװ�ز���
                {
                    robotGroup1 = subCMD[1];
                }
                else if (subCMD[0] == "NOP")  //Ĭ�ϳ����Ե�һ��NOP��ʼ
                {
                    textLines.Add(nextLine);
                    lengthProg++;
                    break;
                }

                headLines.Add(nextLine);
                lengthHead++;
            }

            while ((nextLine = sr.ReadLine()) != null)  //�ٶ�������
            {
                textLines.Add(nextLine);
                lengthProg++;
            }

            //��ȡ��������
            LoadCoordArray();
            sr.Close();
        }

        /// <summary>
        /// ���湤���ļ� ToString("D4")
        /// </summary>
        public void saveJBI(string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            JobName = fileName;

            headLines[1] = @"//NAME "+name;
            headLines[2] = @"//POS ";
            headLines[3] = @"///NPOS " + NumPos.ToString() + ",0,0,0,0,0";
            headLines[4] = @"///USER " + NumUser.ToString();
            headLines[5] = @"///TOOL " + NumTool.ToString();
            headLines[6] = @"///POSTYPE " + posType;
            headLines[headLines.Count - 4] = @"///DATE " + DateTime.Now.ToLongDateString() + DateTime.Now.ToLongTimeString();
            headLines[headLines.Count - 3] = @"///ATTR " + "SC,RW,RJ";
            //headLines[headLines.Count - 2] = @"////FRAME ";
            headLines[headLines.Count - 1] = @"///GROUP1 " + robotGroup1;

            string str = "";
            int indexC = 0;
            for (int i = 0; i < headLines.Count; i++)
            {
                str = headLines[i].ToString();
                
                //�������б���������򣬷�ֹ�����/ɾ��ʾ�̵������
                if (str.StartsWith("C") && !str.StartsWith("CALL"))  //�ҵ�������
                {
                    str = str.Remove(0, 5);
                    str = "C" + indexC.ToString("D4") + str;
                    indexC++;
                }
                sw.WriteLine(str);
            }
            for (int i = 0; i < textLines.Count; i++)
            {
                sw.WriteLine(textLines[i]);
            }
            sw.Close();
            fs.Close();
        }

        /// <summary>
        /// �رչ����ļ�
        /// </summary>
        public void Close()
        {
            JobName = null;
            headLines.Clear();
            textLines.Clear();
            lengthHead = 0;  //��¼�ļ�ͷ����
            lengthProg = 0;  //��¼����������
            NumPos = 0;  //���ݵ����
            IndexPos = -1;  //��ǰ���ݵ�����
            NumUser = 1;  //�û�����͹��߱��
            NumTool = 1;
            posType = "Joint";  //λ������
        }
        
        /// <summary>
        ///������������
        ///</summary>
        private int LoadCoordArray()
        {
            int Indexfirst = 0, Indexlast = 0;  //�������������β
            string str;  //��ʱ�ַ���
            bool found = false; //��¼�Ƿ��ҵ��Ѵ��ڵ��������У�trueΪ�ҵ�
            MathFun.JPOStyp pos = new MathFun.JPOStyp();  //��ʱ���ݱ���

            for (int i = 0; i < lengthHead; i++)  //��¼��ĩ���������е�������
            {
                str = headLines[i].ToString();
                string[] subCMD = str.Split(' ');
                
                if (subCMD[0] == "C0000")  //�ҵ�������
                {
                    Indexfirst = i;
                    found = true;
                }

                if (subCMD[0].Substring(0, 1) == "C" && subCMD[0].Length == 5)
                    Indexlast = i;
                else if (found)  //�������������˳�
                    break;
            }

            if (!found) return 0;  //û�������������߼����򣩣�

            NumPos = Indexlast - Indexfirst + 1;

            poses.Clear();  //������ݵ�
            for (int i = Indexfirst; i <= Indexlast; i++)  //������������
            {
                str = headLines[i].ToString();
                str = str.Remove(0, 6);
                string[] subCMD = str.Split(',');

                try
                {
                    pos.J1 = double.Parse(subCMD[0]);
                    pos.J2 = double.Parse(subCMD[1]);
                    pos.J3 = double.Parse(subCMD[2]);
                    pos.J4 = double.Parse(subCMD[3]);
                    poses.Add(pos);
                }
                catch
                {
                    //MessageBox.Show("������ʽ����", "����");
                    return -1;
                }
            }

            return 0;
        }

        /// <summary>
        /// ����һ�г���
        /// </summary>
        public void insLine(int index, string Line)
        {
            textLines.Insert(index, Line);
            lengthProg++;
        }

        /// <summary>
        /// ɾ��һ�г���
        /// </summary>
        public void delLine(int index)
        {
            textLines.RemoveAt(index);
            lengthProg--;
        }

        /// <summary>
        /// ����һ�����ݵ� ToString("F6") index + 9?
        /// </summary>
        public void insPos(int index, MathFun.JPOStyp pos)
        {
            if ((index < 0) || (index > NumPos))
                return;
            poses.Insert(index, pos);
            string str = "C" + index.ToString("D4") + " " + pos.J1.ToString("F6") + "," + pos.J2.ToString("F6") + "," + pos.J3.ToString("F6") + "," + pos.J4.ToString("F6");
            headLines.Insert(index + 9, str);
            NumPos++;
            lengthHead++;
        }

        /// <summary>
        /// ɾ��һ�����ݵ�
        /// </summary>
        public void delPos(int index)
        {
            if ((index < 0) || (index >= NumPos))
                return;
            poses.RemoveAt(index);
            headLines.RemoveAt(index + 9);
            NumPos--;
            lengthHead--;
        }
    }
}
