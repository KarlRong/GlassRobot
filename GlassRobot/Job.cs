using System.IO;
using System;
using System.Collections;
using Controller;

namespace GlassRobot
{
    public class Job
    {
        public string JobName = null;  //工作文件名

        public ArrayList headLines = new ArrayList();  //定义动态数组保存文件头
        public ArrayList textLines = new ArrayList();  //定义动态数组保存程序行（可显示部分）
        public int lengthHead = 0;  //记录文件头长度
        public int lengthProg = 0;  //记录程序总行数
        
        public ArrayList poses = new ArrayList();  //定义动态数组保存数据点
        public int NumPos = 0;  //数据点个数
        public int IndexPos = -1;  //当前数据点索引
        
        public int NumUser = 1, NumTool = 1;  //用户坐标和工具编号
        public string name = ""; //文件别名
        public string posType;  //位姿类型
        public string robotGroup1;

        /// <summary>
        /// 读取工作文件 文件流操作
        /// </summary>
        public void loadJBI(string fileName)
        {
            StreamReader sr = File.OpenText(fileName);
            JobName = fileName;
            string nextLine;  //读入行

            headLines.Clear();
            textLines.Clear();
            lengthHead = 0;  //记录文件头长度
            lengthProg = 0;  //记录程序总行数
            NumPos = 0;  //数据点个数
            IndexPos = 0;  //当前数据点索引
            NumUser = 1;  //用户坐标和工具编号
            NumTool = 1;
            posType = "Joint";  //位姿类型

            while ((nextLine = sr.ReadLine()) != null)  //先读文件头
            {
                string[] subCMD = nextLine.Split(' ');
                if (subCMD[0] == @"//NAME")  //获取位姿坐标数
                {
                    name = subCMD[1];
                }
                else if (subCMD[0] == @"///NPOS")  //获取位姿坐标数
                {
                    NumPos = int.Parse(subCMD[1].Substring(0,subCMD[1].IndexOf(",")));
                }
                else if (subCMD[0] == @"///USER")  //获取工作文件所用坐标
                {
                    NumUser = int.Parse(subCMD[1]);
                }
                else if (subCMD[0] == @"///TOOL")  //获取工具类型
                {
                    NumTool = int.Parse(subCMD[1]);
                }
                else if (subCMD[0] == @"///POSTYPE")  //获取位姿类型
                {
                    posType = subCMD[1];
                }
                else if (subCMD[0] == @"///GROUP1")//获取装载参数
                {
                    robotGroup1 = subCMD[1];
                }
                else if (subCMD[0] == "NOP")  //默认程序以第一个NOP开始
                {
                    textLines.Add(nextLine);
                    lengthProg++;
                    break;
                }

                headLines.Add(nextLine);
                lengthHead++;
            }

            while ((nextLine = sr.ReadLine()) != null)  //再读程序体
            {
                textLines.Add(nextLine);
                lengthProg++;
            }

            //获取坐标序列
            LoadCoordArray();
            sr.Close();
        }

        /// <summary>
        /// 保存工作文件 ToString("D4")
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
                
                //对数据行标号重新排序，防止因插入/删除示教点而混乱
                if (str.StartsWith("C") && !str.StartsWith("CALL"))  //找到特征行
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
        /// 关闭工作文件
        /// </summary>
        public void Close()
        {
            JobName = null;
            headLines.Clear();
            textLines.Clear();
            lengthHead = 0;  //记录文件头长度
            lengthProg = 0;  //记录程序总行数
            NumPos = 0;  //数据点个数
            IndexPos = -1;  //当前数据点索引
            NumUser = 1;  //用户坐标和工具编号
            NumTool = 1;
            posType = "Joint";  //位姿类型
        }
        
        /// <summary>
        ///载入坐标序列
        ///</summary>
        private int LoadCoordArray()
        {
            int Indexfirst = 0, Indexlast = 0;  //坐标点数据区首尾
            string str;  //临时字符串
            bool found = false; //记录是否找到已存在的坐标序列，true为找到
            MathFun.JPOStyp pos = new MathFun.JPOStyp();  //临时数据变量

            for (int i = 0; i < lengthHead; i++)  //记录首末坐标序列行的行索引
            {
                str = headLines[i].ToString();
                string[] subCMD = str.Split(' ');
                
                if (subCMD[0] == "C0000")  //找到首数据
                {
                    Indexfirst = i;
                    found = true;
                }

                if (subCMD[0].Substring(0, 1) == "C" && subCMD[0].Length == 5)
                    Indexlast = i;
                else if (found)  //数据区结束，退出
                    break;
            }

            if (!found) return 0;  //没有数据区（纯逻辑程序））

            NumPos = Indexlast - Indexfirst + 1;

            poses.Clear();  //清空数据点
            for (int i = Indexfirst; i <= Indexlast; i++)  //载入坐标序列
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
                    //MessageBox.Show("参数格式有误！", "错误！");
                    return -1;
                }
            }

            return 0;
        }

        /// <summary>
        /// 插入一行程序
        /// </summary>
        public void insLine(int index, string Line)
        {
            textLines.Insert(index, Line);
            lengthProg++;
        }

        /// <summary>
        /// 删除一行程序
        /// </summary>
        public void delLine(int index)
        {
            textLines.RemoveAt(index);
            lengthProg--;
        }

        /// <summary>
        /// 插入一个数据点 ToString("F6") index + 9?
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
        /// 删除一个数据点
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
