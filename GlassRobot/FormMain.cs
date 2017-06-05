using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Xml;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using Controller;

namespace GlassRobot
{
    public partial class FormMain : Form
    {
        //���幫������=======================================================================================
        #region
        public Motion motion; //�˶�����������

        public const double KAngle = 57.29577951;//ÿ���ȶ�Ӧ�Ķ���

        public bool modified = false;//�ı��仯���
        public string Password = "10086";//����
        public string NetIP = "";//����IP
        public string NetPort = "";//����˿�

        public Job curJob = new Job();  //��ǰ���ļ�

        public int[] B = new int[100];//Ԥ����������飬���Ÿ��û�ʹ��

        public int[] Indexlabel = new int[200];//���ڴ�ű�������е�����

        public int ontime = 0, runtime = 0, time1m = 0, time5s = 0;  //�ϵ�ʱ�������ʱ�䣬1���Ӽ�������5���Ӽ�����
        public int CThistory = 0, CTtoday = 0, CTtemp = 0;  //�����Ƽ�����ʷ���������ο���������������ǰ���������㣩

        public XmlDocument fileJob = new XmlDocument();  //�����˹����ļ�
        public XmlDocument filePRM = new XmlDocument();  //�����˲����ļ�
        public XmlDocument fileERR = new XmlDocument();  //�����˴�����Ϣ

        public MathFun.EPOStyp[] globalPos = new MathFun.EPOStyp[100];//������ȫ��λ�ñ���
        MathFun.EPOStyp[] PEpos = new MathFun.EPOStyp[100];//�������˶�������ʱλ�ñ���

        public bool recorded = false;  //�����¼��־
        public bool saved = false;  //�Ƿ��¼��ǰλ�ã���¼�����ƶ�ʧЧ��ʾ��������ʧЧ
        public Var.ManuSpeed manuSpeed = Var.ManuSpeed.Low;  //��ǰ�ֶ��ٶ�
        public Var.CurrentMode currentMode = Var.CurrentMode.Teach;  //�����˵�ǰģʽ��ʾ��/�Զ���
        public Var.PlayMode playMode = Var.PlayMode.Cycle;  //�Զ�����ģʽ
        public Var.CurrentStatus currentStatus = Var.CurrentStatus.Stop;  //�����˵�ǰ״̬
        public Var.CompileMode CompMode = Var.CompileMode.Run;
        public int errorCode = 0;  //�������
        public int errorForm = 0;
        public int errorMath = 0;
        public bool run = false;  //��������
        public bool IOrefreshLock = false;  //IOˢ������

        public int indexPos = 0;  //�ı��༭��ȱʡ��ʾ�̵��������ٶ�
        public double defaultVJ = 0, defaultVL = 0;

        #endregion

        public FormMain()
        {
            InitializeComponent();
        }


        //�����ڳ�ʼ�� RC_IniRobot InitMathfun
        private void FormMain_Load(object sender, EventArgs e)
        {
            //����motion����
            motion = new Motion();

            //�򿪲����ļ�������ȫ������
            try
            {
                filePRM.Load(Var.FilePath + "Robot.PRM");
                fileERR.Load(Var.FilePath + "Error.RCD");
            }
            catch
            {
                MessageBox.Show("�޷���ϵͳ�ļ���", "���ش���");
                this.Close();
                return;
            }
                
            if (readAllParameter() != 0)  //�������в�������ʼ�����н���
            {
                MessageBox.Show("�޷�����ϵͳ������", "���ش���");
                this.Close();
                return;
            }

            //�˶���������ʼ��
            motion.errorMotion = motion.RC_IniRobot();
            if (motion.errorMotion != 0)
            {
                MessageBox.Show("�˶���������ʼ��ʧ��", "���ش���");
                this.Close();
                return;
            }

            //��ʼ���˶�������
            InitMathfun();

            //��ȡ���Ա�����
            double[] encData = new double[8];
            if (ReadEncode(ref encData) == 0)
            {
                if (motion.RC_SetPos(encData) == -1)
                    MessageBox.Show(Var.errMsgofMotion[10], "���ش���");
            }
            else
                MessageBox.Show("��ȡ���Ա���������", "���棡");
            
            //�����������ļ�
            curJob.loadJBI(@"\Hard Disk\GlassRobot\Job1.JBI");
            lbfile.Items.Clear();
            for (int i = 0; i < curJob.lengthProg; i++)
            {
                lbfile.Items.Add(curJob.textLines[i].ToString());
            }
            tbJobName.Text = curJob.name;
            labelSysInfo.Text = "��ʾ��" + curJob.name + "�ļ��Ѽ���";
            labelFile.Text = "�ر�";
            if (curJob.NumPos > 0)
            {
                curJob.IndexPos = 0;
                btnCurCPos_Click(null, null);
            }

            
            //�趨��ʼҳ��
            tabControl.SelectedIndex = 2;
            panelEncoder.Visible = false;
            if (SYSPLC.Mod == 1)
            {
                currentMode = Var.CurrentMode.Teach;
                panelManu.Visible = true;
                panelRun.Visible = false;
            }
            else if (SYSPLC.Mod == 2)
            {
                currentMode = Var.CurrentMode.Remote;
                panelManu.Visible = false;
                panelRun.Visible = true;
            }
            else if (SYSPLC.Mod == 3)
            {
                currentMode = Var.CurrentMode.Play;
                panelManu.Visible = false;
                panelRun.Visible = true;
            }
        }

        // �˳������� RC_MovStop RC_Close
        private void FormMain_Closing(object sender, CancelEventArgs e)
        {
            motion.RC_MovStop(false);
            SYSPLC.TboxIOW = ~0xffff;
            Gts.GT_SetExtIoValue(0, (ushort)SYSPLC.TboxIOW, (byte)7);  //ʾ�̺�ָʾ��
            //Gts.GT_SetVal_eHMI((short)0, (ushort)SYSPLC.TboxIOW);

            motion.RC_Close();
            Sv_SysInfo();  //����ϵͳ��Ϣ
        }

        //���ڹرգ����˳���̨�̡߳��ر��ŷ�����δ��
        private void buttonQuit_Click(object sender, EventArgs e)
        {
            bool close = remind();
            if (!close)
                return;
            this.Close();
        }

        //��������˵�ͼƬ��Ӧ=================================
        #region

        //��/�رչ����ļ�
        private void picFileOpen_Click(object sender, EventArgs e)
        {
            if (run)
            {
                labelSysInfo.Text = "��ʾ���������У����ɲ�����ǰ�ļ�������ֹͣ";
                return;
            }
            if (labelFile.Text == "��")  //���ļ�
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK) //���ļ�
                {
                    curJob.loadJBI(openFileDialog.FileName);
                    lbfile.Items.Clear();
                    for (int i = 0; i < curJob.lengthProg; i++)
                    {
                        lbfile.Items.Add(curJob.textLines[i].ToString());
                    }
                    tbJobName.Text = curJob.name;
                    labelSysInfo.Text = "��ʾ��" + curJob.name + "�ļ��Ѽ���";
                    labelFile.Text = "�ر�";
                    if (curJob.NumPos > 0)
                    {
                        curJob.IndexPos = 0;
                        btnCurCPos_Click(null, null);
                    }
                }
            }
            else  //�رյ�ǰ�ļ�
            {
                bool close = remind();
                if (!close)
                    return;
                lbfile.Items.Clear();
                curJob.Close();
                btnCurCPos.Text = "C----";
                picSaveAs.Enabled = false;
                picInsert.Enabled = false;
                picDelete.Enabled = false;
                textCMD.Enabled = false;
                labelSysInfo.Text = "ϵͳ��ʾ��Ϣ";
                labelFile.Text = "��";
                tbJobName.Text = "";
            }
        }

        //��ʼ�༭�����༭Ȩ�ޣ�
        private void picEdit_Click(object sender, EventArgs e)//�༭�ļ�,����˰�ť��������Enabled Ϊ false�����Ϊtrue��������Ϊtrue��������༭���߶Ե�ǰ�н��б༭��
        {
            if (picSaveAs.Enabled == false)
            {
                string pass = KeyNum("������������룺", "", true);
                if (pass != Password)
                {
                    MessageBox.Show("�������");
                    return;
                }
                picSaveAs.Enabled = true;
                picInsert.Enabled = true;
                picDelete.Enabled = true;
                textCMD.Enabled = true;
            }
            else  //�ر�Ȩ��
            {
                picSaveAs.Enabled = false;
                picInsert.Enabled = false;
                picDelete.Enabled = false;
                textCMD.Enabled = false;
            }
        }

        //�����ļ� FileName
        private void picSaveAs_Click(object sender, EventArgs e)//����ļ�
        {
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                curJob.name = saveFileDialog.FileName.Remove(0, saveFileDialog.FileName.LastIndexOf('\\')+1);
                curJob.saveJBI(saveFileDialog.FileName);
                modified = false;
                tbJobName.Text = saveFileDialog.FileName;
            }
        }

        //�����޸� Sleep(50)
        private void pbSet_Click(object sender, EventArgs e)
        {
            string pass = "";
            string secpass = "";
            DialogResult result = MessageBox.Show("Ҫ�޸ĵ�ǰ������", "��ʾ", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (result == DialogResult.Yes)
            {
                pass = KeyNum("������ɵĹ������룺", "", true);
                if (pass != Password)  //��ʼ����10086
                {
                    MessageBox.Show("�������,�����²���");
                    return;
                }
                else  //�ر�Ȩ��
                {
                    pass = KeyNum("�������µĹ������룺", "", true);
                    Thread.Sleep(50);
                    secpass = KeyNum("�����������µĹ������룺", "", true);
                    if (pass == secpass)
                    {
                        Password = pass;
                        Sv_Password();
                        MessageBox.Show("���������޸ĳɹ�");
                    }
                    else
                    {
                        MessageBox.Show("���������޸�ʧ�ܣ������²���");
                    }
                }
            }
        }

        //�����趨
        private void picTool_Click(object sender, EventArgs e)
        {
            panelTool.Visible = true;
            panelServo.Visible = false;
            panelNet.Visible = false;
        }

        //�ŷ������趨
        private void picServoParam_Click(object sender, EventArgs e)
        {
            panelTool.Visible = false;
            panelServo.Visible = true;
            panelNet.Visible = false;
        }

        //��������趨
        private void picNetwork_Click(object sender, EventArgs e)
        {
            panelTool.Visible = false;
            panelServo.Visible = false;
            panelNet.Visible = true;
        }

        //�������в���
        private void picSaveParam_Click(object sender, EventArgs e)
        {
            App_Robot();//���沢����Ӧ��
        }

        //��ȡ���Ա���������
        private void picEncoder_Click(object sender, EventArgs e)
        {
            panelEncoder.Visible = true;
            panelManu.Visible = false;
            panelRun.Visible = false;
        }

        //�Զ�����
        private void picPlay_Click(object sender, EventArgs e)
        {
            //if ((currentMode == Var.CurrentMode.Teach) || (currentMode == Var.CurrentMode.Remote))
            //{
            //    if (!motion.teachLock)  //û��������ʾ��״̬
            //    {
                    currentMode = Var.CurrentMode.Play;
                    picOpMod.Image = Properties.Resources.AUTO;
                    panelEncoder.Visible = false;
                    panelManu.Visible = false;
                    panelRun.Visible = true;
            //    }
            //    else errorCode = 99;  //����ʾ�̣������л�������
            //}
        }

        //�ֶ�����
        private void picTeach_Click(object sender, EventArgs e)
        {
            if (!run)  //û����������
            {
                currentMode = Var.CurrentMode.Teach;
                picOpMod.Image = Properties.Resources.HAND;

                panelEncoder.Visible = false;
                panelManu.Visible = true;
                panelRun.Visible = false;
            }
            else errorCode = 99;  //�����Զ����У������л�������
        }

        //ϵͳIO����
        private void picSysIO_Click(object sender, EventArgs e)
        {
            panelSysIO.Visible = true;
            panelUserIO.Visible = false;
        }

        //�û�IO����
        private void picUserIO_Click(object sender, EventArgs e)
        {
            panelSysIO.Visible = false;
            panelUserIO.Visible = true;
        }

        //ͳ����Ϣ����
        private void picCountInfo_Click(object sender, EventArgs e)
        {
            panelCountInfo.Visible = true;
            panelErrList.Visible = false;
            panelBackup.Visible = false;
            panelVersionInfo.Visible = false;
        }

        //������Ϣ����
        private void picErrList_Click(object sender, EventArgs e)
        {
            textBoxErrList.Text = "";
            Ld_ErrorMsg();
            if (textBoxErrList.Text.Length > 0)
                textBoxErrList.SelectionStart = textBoxErrList.Text.Length - 1;
            panelCountInfo.Visible = false;
            panelErrList.Visible = true;
            panelBackup.Visible = false;
            panelVersionInfo.Visible = false;
        }

        //���ݽ���
        private void picBackUp_Click(object sender, EventArgs e)
        {
            panelCountInfo.Visible = false;
            panelErrList.Visible = false;
            panelBackup.Visible = true;
            panelVersionInfo.Visible = false;
        }

        //�汾��Ϣ����
        private void picVer_Click(object sender, EventArgs e)
        {
            panelCountInfo.Visible = false;
            panelErrList.Visible = false;
            panelBackup.Visible = false;
            panelVersionInfo.Visible = true;
        }

        //������Ϣ����
        private void picHelp_Click(object sender, EventArgs e)
        {
            //Process helpprocess = new Process();
            //helpprocess.StartInfo.FileName = "PegHelp.exe";
            //helpprocess.StartInfo.Arguments = Var.HelpPath;
            //helpprocess.Start();
        }

        #endregion

        //��״̬ͼƬ��Ӧ=======================================
        #region

        //״̬��ʾ�������ڴ˸ı�״̬���Է��������

        //�л�������������ʾ��״̬���ã� motoin.tool
        private void picManipulator_Click(object sender, EventArgs e)
        {
            if (motion.robotNum == 1)
            {
                motion.robotNum = 2;
                picManipulator.Image = Properties.Resources.ROBOT2;
                SYSPLC.TB_robotgroup = true;
                motion.tool.X = motion.tool2.X;
                motion.tool.Z = motion.tool2.Z;
                MathFun.TOOL = motion.tool;
            }
            else if (motion.robotNum == 2)
            {
                motion.robotNum = 1;
                picManipulator.Image = Properties.Resources.ROBOT1;
                SYSPLC.TB_robotgroup = false;
                motion.tool.X = motion.tool1.X;
                motion.tool.Z = motion.tool1.Z;
                MathFun.TOOL = motion.tool;
            }
        }

        //�л�����ϵ������ʾ��״̬���ã� ����
        private void picCoord_Click(object sender, EventArgs e)
        {
            if (currentMode != Var.CurrentMode.Teach)
                return;

            if (motion.coord == MathFun.COORDtyp.Robot)
            {
                motion.coord = MathFun.COORDtyp.Joint;
                motion.dirPos.C = MathFun.COORDtyp.Joint;
                picCoord.Image = Properties.Resources.COORDROBOT;

                picAN.Image = Properties.Resources.AN;
                picAP.Image = Properties.Resources.AP;
                picBN.Image = Properties.Resources.BN;
                picBP.Image = Properties.Resources.BP;
                picCN.Image = Properties.Resources.CN;
                picCP.Image = Properties.Resources.CP;
                picDN.Image = Properties.Resources.DN;
                picDP.Image = Properties.Resources.DP;
            }
            else if (motion.coord == MathFun.COORDtyp.Joint)
            {
                motion.coord = MathFun.COORDtyp.Robot;
                motion.dirPos.C = MathFun.COORDtyp.Robot;
                picCoord.Image = Properties.Resources.COORDCARTESIAN;

                picAN.Image = Properties.Resources.XN;
                picAP.Image = Properties.Resources.XP;
                picBN.Image = Properties.Resources.ZN;
                picBP.Image = Properties.Resources.ZP;
                picCN.Image = Properties.Resources.RN;
                picCP.Image = Properties.Resources.RP;
                picDN.Image = Properties.Resources.RzN;
                picDP.Image = Properties.Resources.RzP;
            }
        }

        //�л��ٶȣ�����ʾ��״̬�� speedJ speedL
        private void picSpeed_Click(object sender, EventArgs e)
        {
            if (currentMode != Var.CurrentMode.Teach)
                return;

            if (manuSpeed == Var.ManuSpeed.Inc)
            {
                manuSpeed = Var.ManuSpeed.Low;
                motion.speedJ = Var.speedL;
                motion.speedL = motion.MaxSpeedL * Var.speedL;
                picSpeed.Image = Properties.Resources.SPEEDLOW;
            }
            else if (manuSpeed == Var.ManuSpeed.Low)
            {
                manuSpeed = Var.ManuSpeed.Middle;
                motion.speedJ = Var.speedM;
                motion.speedL = motion.MaxSpeedL * Var.speedM;
                picSpeed.Image = Properties.Resources.SPEEDHIGH;
            }
            else if (manuSpeed == Var.ManuSpeed.Middle)
            {
                manuSpeed = Var.ManuSpeed.High;
                motion.speedJ = Var.speedH;
                motion.speedL = motion.MaxSpeedL * Var.speedH;
                picSpeed.Image = Properties.Resources.SPEEDMAX;
            }
            else if (manuSpeed == Var.ManuSpeed.High)
            {
                manuSpeed = Var.ManuSpeed.Inc;
                motion.speedJ = Var.speedI;
                motion.speedL = motion.MaxSpeedL * Var.speedI;
                picSpeed.Image = Properties.Resources.SPEEDINC;
            }
        }

        //�ֶ�/�Զ��л�������ʾ�̺���ť���ƣ�������治�ɸ��ģ��Է��������
        private void picOpMod_Click(object sender, EventArgs e)
        {
            //if (currentMode == Var.CurrentMode.Teach)
            //    picPlay_Click(null, null);
            //else if (currentMode == Var.CurrentMode.Play)
            //    picTeach_Click(null, null);
        }

        //ʾ�������л�������ʾ��״̬�� motion.teachLock
        private void picTeachLock_Click(object sender, EventArgs e)
        {
            if (currentMode != Var.CurrentMode.Teach)
                return;

            if (motion.teachLock)
            {
                motion.teachLock = false;
                picTeachLock.Image = Properties.Resources.TUNLOCK;
            }
            else
            {
                motion.teachLock = true;
                picTeachLock.Image = Properties.Resources.TLOCK;
            }
        }

        //�ŷ���Դ״̬��ʾ�������ڴ˸ı�״̬���Է��������

        //���з�ʽ�л�������/����/ѭ����playMode
        private void picCycle_Click(object sender, EventArgs e)
        {
            if (playMode == Var.PlayMode.Auto)
            {
                playMode = Var.PlayMode.Cycle;
                picCycle.Image = Properties.Resources.CYCLERUN;
            }
            else if (playMode == Var.PlayMode.Cycle)
            {
                playMode = Var.PlayMode.Step;
                picCycle.Image = Properties.Resources.POINTRUN;
            }
            else if (playMode == Var.PlayMode.Step)
            {
                playMode = Var.PlayMode.Auto;
                picCycle.Image = Properties.Resources.AUTORUN;
            }
        }

        #endregion

        //�����ļ����������===================================
        #region

        //����һ�г���
        private void picInsert_Click(object sender, EventArgs e)
        {
            lbfile.Items.Insert(lbfile.SelectedIndex + 1, textCMD.Text);
            curJob.insLine(lbfile.SelectedIndex + 1, textCMD.Text);
            lbfile.SelectedIndex++;
            modified = true;  //�����޸�
        }

        //ɾ��һ�г���
        private void picDelete_Click(object sender, EventArgs e)
        {
            if (lbfile.SelectedIndex != -1)
            {
                curJob.delLine(lbfile.SelectedIndex);
                lbfile.SelectedIndex--;
                lbfile.Items.RemoveAt(lbfile.SelectedIndex + 1);
                modified = true;
            }
        }

        //����������ý��㣨������������������б�
        private void textCMD_GotFocus(object sender, EventArgs e)
        {
            lbCmd.Visible = true;  //���������б�
            lbCmd.Focus();  //�������б�õ�����
            if (lbCmd.SelectedIndex == -1)
                lbCmd.SelectedIndex = 2;
            SYSPLC.TB_informlist = true;
        }

        //�������б�ʧȥ����
        private void lbCmd_LostFocus(object sender, EventArgs e)
        {
            if (lbCmd.Visible)
                lbCmd.Focus();  //�������б�ǿ�Ƶõ�����
        }

        //�������б�����
        private void lbCmd_KeyDown(object sender, KeyEventArgs e)
        {
            int keynum = (int)e.KeyData;
            switch (keynum)
            {
                case 27:  //Esc
                case 69:  //E������һ��
                    lbCmd.Visible = false;
                    SYSPLC.TB_informlist = false;
                    break;
                case 115:  //F4��ѡ��
                case 89:  //Y��ȷ��
                    lbCmdPutin();
                    lbCmd.Visible = false;
                    SYSPLC.TB_informlist = false;
                    break;
                default:
                    break;
            }
        }

        //�����ļ���ʾ���ڶ԰����ķ�Ӧ //ʲô��������ϣ�
        private void lbfile_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        #endregion

        //ϵͳ���ø����������ȡ������=========================
        #region

        //panelTool�е��ı���ý��¼�
        private void textBoxCatchTime_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxCatchTime.Text = KeyNum("��������צ����ʱ�䣺", textBoxCatchTime.Text, false);
        }

        private void textBoxReleaseTime_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxReleaseTime.Text = KeyNum("��������צ�ͷ�ʱ�䣺", textBoxReleaseTime.Text, false);
        }
        
        private void textBoxHandIN1_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxHandIN1.Text = KeyNum("��������צ����˿ڣ�", textBoxHandIN1.Text, false);
        }
        
        private void textBoxHandOUT1_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxHandOUT1.Text = KeyNum("��������צ����˿ڣ�", textBoxHandOUT1.Text, false);
        }
        
        private void textBoxHandX_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxHandX.Text = KeyNum("��������������X��", textBoxHandX.Text, false);
        }

        private void textBoxHandY_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxHandY.Text = KeyNum("��������������Y��", textBoxHandY.Text, false);
        }

        private void textBoxHandZ_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxHandZ.Text = KeyNum("��������������Z��", textBoxHandZ.Text, false);
        }

        //panelServo�е��ı���ý��¼�
        private void textBoxLimitAP_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxLimitAP.Text = KeyNum("������A������λ��", textBoxLimitAP.Text, false);
        }

        private void textBoxLimitAN_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxLimitAN.Text = KeyNum("������A�Ḻ��λ��", textBoxLimitAN.Text, false);
        }

        private void textBoxLimitBP_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxLimitBP.Text = KeyNum("������B������λ��", textBoxLimitBP.Text, false);
        }

        private void textBoxLimitBN_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxLimitBN.Text = KeyNum("������B�Ḻ��λ��", textBoxLimitBN.Text, false);
        }

        private void textBoxLimitCP_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxLimitCP.Text = KeyNum("������C������λ��", textBoxLimitCP.Text, false);
        }

        private void textBoxLimitCN_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxLimitCN.Text = KeyNum("������C�Ḻ��λ��", textBoxLimitCN.Text, false);
        }

        private void textBoxLimitDP_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxLimitDP.Text = KeyNum("������D������λ��", textBoxLimitDP.Text, false);
        }

        private void textBoxLimitDN_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxLimitDN.Text = KeyNum("������D��������λ��", textBoxLimitDN.Text, false);
        }

        private void textBoxVmaxA_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxVmaxA.Text = KeyNum("������A������ٶȣ�", textBoxVmaxA.Text, false);
        }

        private void textBoxVmaxB_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxVmaxB.Text = KeyNum("������B������ٶȣ�", textBoxVmaxB.Text, false);
        }

        private void textBoxVmaxC_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxVmaxC.Text = KeyNum("������C������ٶȣ�", textBoxVmaxC.Text, false);
        }

        private void textBoxVmaxD_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxVmaxD.Text = KeyNum("������D������ٶȣ�", textBoxVmaxD.Text, false);
        }

        private void textBoxAmaxA_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxAmaxA.Text = KeyNum("������A�������ٶȣ�", textBoxAmaxA.Text, false);
        }

        private void textBoxAmaxB_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxAmaxB.Text = KeyNum("������B�������ٶȣ�", textBoxAmaxB.Text, false);
        }

        private void textBoxAmaxC_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxAmaxC.Text = KeyNum("������C�������ٶȣ�", textBoxAmaxC.Text, false);
        }

        private void textBoxAmaxD_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxAmaxD.Text = KeyNum("������D�������ٶȣ�", textBoxAmaxD.Text, false);
        }

        //panelNet�е��ı���ý��¼�
        private void textBoxIP_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxIP.Text = KeyNum("������IP��ַ��", textBoxIP.Text, false);
        }

        private void textBoxPort_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxPort.Text = KeyNum("������˿ںţ�", textBoxPort.Text, false);
        }

        #endregion

        //ʾ�����и�����ؼ���Ӧ===============================

        //��ȡ���Ա����� ReadEncode
        private void btnreadall_Click(object sender, EventArgs e)
        {
            //��ȡ���Ա�����
            double[] encData = new double[8];
        
            if (ReadEncode(ref encData) == 0)
            {
                short rtn = motion.RC_SetPos(encData);
                if (rtn == -1)
                {
                    MessageBox.Show(Var.errMsgofMotion[10], "���ش���");
                }
                else
                {
                    for (int i = 0; i < 8; i++)  //ת��Ϊ���ȵ�λ
                        encData[i] = encData[i] / (-MathFun.KJ[i]);
                    tbJ1.Text = encData[0].ToString();
                    tbJ2.Text = encData[1].ToString();
                    tbJ3.Text = encData[2].ToString();
                    tbJ4.Text = encData[3].ToString();
                    tbJ5.Text = encData[4].ToString();
                    tbJ6.Text = encData[5].ToString();
                    tbJ7.Text = encData[6].ToString();
                    tbJ8.Text = encData[7].ToString();
                }
            }
            else
            {
                MessageBox.Show("��ȡ���Ա���������", "���棡");
            }
        }
        
        //����ԭ������λ��Ϣ  MathFun.HJ[]
        private void btnLoadHome_Click(object sender, EventArgs e)
        {
            tbJ1.Text = MathFun.HJ[0].ToString();
            tbJ2.Text = MathFun.HJ[1].ToString();
            tbJ3.Text = MathFun.HJ[2].ToString();
            tbJ4.Text = MathFun.HJ[3].ToString();
            tbJ5.Text = MathFun.HJ[4].ToString();
            tbJ6.Text = MathFun.HJ[5].ToString();
            tbJ7.Text = MathFun.HJ[6].ToString();
            tbJ8.Text = MathFun.HJ[7].ToString();
        }

        //�ѵ�ǰֵ��Ϊԭ�㲢����
        private void btnSetHome_Click(object sender, EventArgs e)
        {
            try
            {
                XmlElement xePRM = filePRM.DocumentElement;
                if (motion.robotNum == 1)
                {
                    MathFun.HJ[0] = double.Parse(tbJ1.Text) + Math.PI / 2;
                    MathFun.HJ[1] = double.Parse(tbJ2.Text);
                    MathFun.HJ[2] = double.Parse(tbJ3.Text);
                    MathFun.HJ[3] = double.Parse(tbJ4.Text);

                    xePRM.SelectSingleNode("/RobotPRM/Home/J1").InnerText = MathFun.HJ[0].ToString();
                    xePRM.SelectSingleNode("/RobotPRM/Home/J2").InnerText = MathFun.HJ[1].ToString();
                    xePRM.SelectSingleNode("/RobotPRM/Home/J3").InnerText = MathFun.HJ[2].ToString();
                    xePRM.SelectSingleNode("/RobotPRM/Home/J4").InnerText = MathFun.HJ[3].ToString();
                }
                else
                {
                    MathFun.HJ[4] = double.Parse(tbJ5.Text) + Math.PI / 2;
                    MathFun.HJ[5] = double.Parse(tbJ6.Text);
                    MathFun.HJ[6] = double.Parse(tbJ7.Text);
                    MathFun.HJ[7] = double.Parse(tbJ8.Text);

                    xePRM.SelectSingleNode("/RobotPRM/Home/J5").InnerText = MathFun.HJ[4].ToString();
                    xePRM.SelectSingleNode("/RobotPRM/Home/J6").InnerText = MathFun.HJ[5].ToString();
                    xePRM.SelectSingleNode("/RobotPRM/Home/J7").InnerText = MathFun.HJ[6].ToString();
                    xePRM.SelectSingleNode("/RobotPRM/Home/J8").InnerText = MathFun.HJ[7].ToString();
                }

                filePRM.Save(Var.FilePath + "Robot.PRM");
            }
            catch
            {
                MessageBox.Show("�����˷Ƿ�������", "����");
            }
        }

        //panelManu �еĿؼ���Ӧ==================

        //�ֶ���ť================================ motion.dirPos.X RC_ManuMov
        #region

        //���������£���ʼ�˶�
        private void picAN_MouseDown(object sender, MouseEventArgs e)
        {

            if (SYSPLC.TB_teach)
            {
                picAP.Enabled = false;
                picBN.Enabled = false;
                picBP.Enabled = false;
                picCN.Enabled = false;
                picCP.Enabled = false;
                picDN.Enabled = false;
                picDP.Enabled = false;

                if ((motion.coord == MathFun.COORDtyp.Joint)) //�ؽ�����
                {
                    picAN.Image = Properties.Resources.ANblue;
                }
                else//����ֱ������
                {
                    picAN.Image = Properties.Resources.XNblue;
                }
                motion.dirPos.X = -1;
                motion.RC_ManuMov();
            }
        }

        //�������ͷţ�ֹͣ�˶�
        private void picAN_MouseUp(object sender, MouseEventArgs e)
        {
            if ((motion.coord == MathFun.COORDtyp.Joint)) //�ؽ�����
            {
                picAN.Image = Properties.Resources.AN;
            }
            else  //����ֱ������
            {
                picAN.Image = Properties.Resources.XN;
            }

            picAP.Enabled = true;
            picBN.Enabled = true;
            picBP.Enabled = true;
            picCN.Enabled = true;
            picCP.Enabled = true;
            picDN.Enabled = true;
            picDP.Enabled = true;

            motion.dirPos = 0 * motion.dirPos;
            motion.RC_MovStop(false);
        }

        private void picAP_MouseDown(object sender, MouseEventArgs e)
        {
            if (SYSPLC.TB_teach)
            {
                picAN.Enabled = false;
                picBN.Enabled = false;
                picBP.Enabled = false;
                picCN.Enabled = false;
                picCP.Enabled = false;
                picDN.Enabled = false;
                picDP.Enabled = false;

                if ((motion.coord == MathFun.COORDtyp.Joint)) //�ؽ�����
                {
                    picAP.Image = Properties.Resources.APblue;
                }
                else  //����ֱ������
                {
                    picAP.Image = Properties.Resources.XPblue;
                }
                motion.dirPos.X = 1;
                motion.RC_ManuMov();
            }
        }

        private void picAP_MouseUp(object sender, MouseEventArgs e)
        {
            if ((motion.coord == MathFun.COORDtyp.Joint)) //�ؽ�����
            {
                picAP.Image = Properties.Resources.AP;
            }
            else  //����ֱ������
            {
                picAP.Image = Properties.Resources.XP;
            }

            picAN.Enabled = true;
            picBN.Enabled = true;
            picBP.Enabled = true;
            picCN.Enabled = true;
            picCP.Enabled = true;
            picDN.Enabled = true;
            picDP.Enabled = true;

            motion.dirPos = 0 * motion.dirPos;
            motion.RC_MovStop(false);
        }

        private void picBN_MouseDown(object sender, MouseEventArgs e)
        {

            if (SYSPLC.TB_teach)
            {
                picAN.Enabled = false;
                picAP.Enabled = false;
                picBP.Enabled = false;
                picCN.Enabled = false;
                picCP.Enabled = false;
                picDN.Enabled = false;
                picDP.Enabled = false;

                if (motion.coord == MathFun.COORDtyp.Joint)  //�ؽ�����
                {
                    picBN.Image = Properties.Resources.BNblue;
                }
                else  //����ֱ������
                {
                    picBN.Image = Properties.Resources.ZNblue;
                }
                motion.dirPos.Y = -1;
                motion.RC_ManuMov();
            }
        }

        private void picBN_MouseUp(object sender, MouseEventArgs e)
        {
            if (motion.coord == MathFun.COORDtyp.Joint)  //�ؽ�����
            {
                picBN.Image = Properties.Resources.BN;
            }
            else  //����ֱ������
            {
                picBN.Image = Properties.Resources.ZN;
            }

            picAN.Enabled = true;
            picAP.Enabled = true;
            picBP.Enabled = true;
            picCN.Enabled = true;
            picCP.Enabled = true;
            picDN.Enabled = true;
            picDP.Enabled = true;

            motion.dirPos = 0 * motion.dirPos;
            motion.RC_MovStop(false);
        }

        private void picBP_MouseDown(object sender, MouseEventArgs e)
        {

            if (SYSPLC.TB_teach)
            {
                picAN.Enabled = false;
                picAP.Enabled = false;
                picBN.Enabled = false;
                picCN.Enabled = false;
                picCP.Enabled = false;
                picDN.Enabled = false;
                picDP.Enabled = false;

                if (motion.coord == MathFun.COORDtyp.Joint)  //�ؽ�����
                {
                    picBP.Image = Properties.Resources.BPblue;
                }
                else  //����ֱ������
                {
                    picBP.Image = Properties.Resources.ZPblue;
                }
                motion.dirPos.Y = 1;
                motion.RC_ManuMov();
            }
        }

        private void picBP_MouseUp(object sender, MouseEventArgs e)
        {
            if (motion.coord == MathFun.COORDtyp.Joint)  //�ؽ�����
            {
                picBP.Image = Properties.Resources.BP;
            }
            else  //����ֱ������
            {
                picBP.Image = Properties.Resources.ZP;
            }

            picAN.Enabled = true;
            picAP.Enabled = true;
            picBN.Enabled = true;
            picCN.Enabled = true;
            picCP.Enabled = true;
            picDN.Enabled = true;
            picDP.Enabled = true;

            motion.dirPos = 0 * motion.dirPos;
            motion.RC_MovStop(false);
        }

        private void picCN_MouseDown(object sender, MouseEventArgs e)
        {

            if (SYSPLC.TB_teach)
            {
                picAN.Enabled = false;
                picAP.Enabled = false;
                picBN.Enabled = false;
                picBP.Enabled = false;
                picCP.Enabled = false;
                picDN.Enabled = false;
                picDP.Enabled = false;

                if (motion.coord == MathFun.COORDtyp.Joint)  //�ؽ�����
                {
                    picCN.Image = Properties.Resources.CNblue;
                }
                else  //����ֱ������
                {
                    picCN.Image = Properties.Resources.RNblue;
                }
                motion.dirPos.Z = -1;
                motion.RC_ManuMov();
            }
        }

        private void picCN_MouseUp(object sender, MouseEventArgs e)
        {
            if (motion.coord == MathFun.COORDtyp.Joint)  //�ؽ�����
            {
                picCN.Image = Properties.Resources.CN;
            }
            else  //����ֱ������
            {
                picCN.Image = Properties.Resources.RN;
            }

            picAN.Enabled = true;
            picAP.Enabled = true;
            picBN.Enabled = true;
            picBP.Enabled = true;
            picCP.Enabled = true;
            picDN.Enabled = true;
            picDP.Enabled = true;

            motion.dirPos = 0 * motion.dirPos;
            motion.RC_MovStop(false);
        }

        private void picCP_MouseDown(object sender, MouseEventArgs e)
        {

            if (SYSPLC.TB_teach)
            {
                picAN.Enabled = false;
                picAP.Enabled = false;
                picBN.Enabled = false;
                picBP.Enabled = false;
                picCN.Enabled = false;
                picDN.Enabled = false;
                picDP.Enabled = false;

                if (motion.coord == MathFun.COORDtyp.Joint)  //�ؽ�����
                {
                    picCP.Image = Properties.Resources.CPblue;
                }
                else  //����ֱ������
                {
                    picCP.Image = Properties.Resources.RPblue;
                }
                motion.dirPos.Z = 1;
                motion.RC_ManuMov();
            }
        }

        private void picCP_MouseUp(object sender, MouseEventArgs e)
        {
            if (motion.coord == MathFun.COORDtyp.Joint)  //�ؽ�����
            {
                picCP.Image = Properties.Resources.CP;
            }
            else  //����ֱ������
            {
                picCP.Image = Properties.Resources.RP;
            }

            picAN.Enabled = true;
            picAP.Enabled = true;
            picBN.Enabled = true;
            picBP.Enabled = true;
            picCN.Enabled = true;
            picDN.Enabled = true;
            picDP.Enabled = true;

            motion.dirPos = 0 * motion.dirPos;
            motion.RC_MovStop(false);
        }

        private void picDN_MouseDown(object sender, MouseEventArgs e)
        {

            if (SYSPLC.TB_teach)
            {
                picAN.Enabled = false;
                picAP.Enabled = false;
                picBN.Enabled = false;
                picBP.Enabled = false;
                picCN.Enabled = false;
                picCP.Enabled = false;
                picDP.Enabled = false;
                if (motion.coord == MathFun.COORDtyp.Joint)  //�ؽ�����
                {
                    picDN.Image = Properties.Resources.DNblue;
                }
                else  //����ֱ������
                {
                    picDN.Image = Properties.Resources.RzNblue;
                }
                motion.dirPos.THETA = -1;
                motion.RC_ManuMov();
            }
        }

        private void picDN_MouseUp(object sender, MouseEventArgs e)
        {

            picAN.Enabled = true;
            picAP.Enabled = true;
            picBN.Enabled = true;
            picBP.Enabled = true;
            picCN.Enabled = true;
            picCP.Enabled = true;
            picDP.Enabled = true;
            if (motion.coord == MathFun.COORDtyp.Joint)  //�ؽ�����
            {
                picDN.Image = Properties.Resources.DN;
            }
            else  //����ֱ������
            {
                picDN.Image = Properties.Resources.RzN;
            }

            motion.dirPos = 0 * motion.dirPos;
            motion.RC_MovStop(false);
        }

        private void picDP_MouseDown(object sender, MouseEventArgs e)
        {

            if (SYSPLC.TB_teach)
            {
                picAN.Enabled = false;
                picAP.Enabled = false;
                picBN.Enabled = false;
                picBP.Enabled = false;
                picCN.Enabled = false;
                picCP.Enabled = false;
                picDN.Enabled = false;
                if (motion.coord == MathFun.COORDtyp.Joint)  //�ؽ�����
                {
                    picDP.Image = Properties.Resources.DPblue;
                }
                else  //����ֱ������
                {
                    picDP.Image = Properties.Resources.RzPblue;
                }
                motion.dirPos.THETA = 1;
                motion.RC_ManuMov();
            }
        }

        private void picDP_MouseUp(object sender, MouseEventArgs e)
        {

            picAN.Enabled = true;
            picAP.Enabled = true;
            picBN.Enabled = true;
            picBP.Enabled = true;
            picCN.Enabled = true;
            picCP.Enabled = true;
            picDN.Enabled = true;
            if (motion.coord == MathFun.COORDtyp.Joint)  //�ؽ�����
            {
                picDP.Image = Properties.Resources.DP;
            }
            else  //����ֱ������
            {
                picDP.Image = Properties.Resources.RzP;
            }

            motion.dirPos = 0 * motion.dirPos;
            motion.RC_MovStop(false);
        }

        #endregion

        //panelHand��������ť�¼�=================
        #region
        //�ŷ�״̬�л� ����ʹ�� RC_ServoOn
        private void buttonServo_Click(object sender, EventArgs e)
        {
            if ((SYSPLC.TB_teach && SYSPLC.TB_deadman) || SYSPLC.TB_play || SYSPLC.TB_remote)
            {
                if (buttonServo.Text == "�ŷ�ʹ��")
                {
                    //if (motion.RC_ServoOn(0xff) != 0)
                    if (motion.RC_ServoOn(0x01) != 0)
                        {
                        Motion.RC_ServoOff(0xff);  //���������ʹ��ʧ�ܣ�ǿ��ȫ��ʧ��
                        labelSysInfo.Text = "��ʾ���ŷ�ʹ��ʧ��";
                    }
                    else
                    {
                        SYSPLC.TB_servoonready = true;
                        buttonServo.Text = "�ŷ�ʧ��";
                        buttonServo.BackColor = System.Drawing.Color.Red;
                        picServo.Image = Properties.Resources.SERVOON;
                        labelSysInfo.Text = "��ʾ���ŷ�ʹ��";
                    }
                }
                else
                {
                    if (Motion.RC_ServoOff(0xff) != 0)
                    {
                        labelSysInfo.Text = "��ʾ���ŷ�ʧ��ʧ��";
                    }
                    else
                    {
                        SYSPLC.TB_servoonready = false;
                        buttonServo.Text = "�ŷ�ʹ��";
                        buttonServo.BackColor = System.Drawing.Color.Green;
                        picServo.Image = Properties.Resources.SERVOOFF;
                        labelSysInfo.Text = "��ʾ���ŷ�ʧ��";
                    }
                }
                if (labelSysInfo.Text == "��ʾ������ʹ�ܿ���δ��")
                    labelSysInfo.Text = "ϵͳ��ʾ��Ϣ";
            }
            else
            {
                labelSysInfo.Text = "��ʾ������ʹ�ܿ���δ��";
                return;
            }

        }

        //������λ���������ڵ��ԣ�
        private void buttonreset_Click(object sender, EventArgs e)
        {
            //buttonreset.BackColor = Color.Gray;
            //run = false;
            //motion.start1 = false;
            //motion.RC_MovStop(false);
            //////����̴߳��ڣ����ȹرգ��½���������
            ////if (motion.threadRun != null)
            ////{
            ////    motion.threadRun.Abort();
            ////    motion.threadRun = null;
            ////}

            ////���¶�ȡ���Ա�����
            //double[] encData = new double[8];
            //if (ReadEncode(ref encData) == 0)
            //{
            //    motion.RC_SetPos(encData);
            //    labelSysInfo.Text = "��ʾ�����óɹ�";
            //}
            //else
            //    labelSysInfo.Text = "��ʾ����ȡ���Ա�����ʧ�ܣ�������";
            //motion.RC_Clear();//�滮ʵ��λ������
            //buttonreset.BackColor = Color.Yellow;
        }

        //��צ״̬�л�
        private void buttonHand_Click(object sender, EventArgs e)
        {
            if (buttonHand.Text == "��צ����")
            {

                buttonHand.Text = "��צ�ͷ�";
                buttonHand.BackColor = System.Drawing.Color.Red;
            }
            else
            {

                buttonHand.Text = "��צ����";
                buttonHand.BackColor = System.Drawing.Color.Green;
            }
        }

        //���Ĺ����ļ�ʾ�̵�����
        private void btnCurCPos_Click(object sender, EventArgs e)
        {
            if (curJob.JobName == null) return;  //û�д򿪵��ļ�������
            if (curJob.NumPos <= 0) return;  //û��ʾ�̵���ļ�������

            MathFun.JPOStyp jPos = new MathFun.JPOStyp();
            jPos = (MathFun.JPOStyp)curJob.poses[curJob.IndexPos];
            textBoxA.Text = (jPos.J1 * KAngle).ToString("F3") + "deg";
            textBoxB.Text = (jPos.J2 * KAngle).ToString("F3") + "deg";
            textBoxC.Text = (jPos.J3 * KAngle).ToString("F3") + "deg";
            textBoxD.Text = (jPos.J4 * KAngle).ToString("F3") + "deg";
            btnCurCPos.Text = "C" + curJob.IndexPos.ToString("D4");
        }

        //��һʾ�̵�
        private void btnPrev_Click(object sender, EventArgs e)
        {
            if (curJob.JobName == null) return;  //û�д򿪵��ļ�������
            if (curJob.IndexPos > 0) curJob.IndexPos--;
            btnCurCPos.Text = "C" + curJob.IndexPos.ToString("D4");
            btnCurCPos_Click(null, null);
        }

        //��һʾ�̵�
        private void btnNext_Click(object sender, EventArgs e)
        {
            if (curJob.JobName == null) return;  //û�д򿪵��ļ�������
            if (curJob.IndexPos < (curJob.NumPos - 1)) curJob.IndexPos++;
            btnCurCPos.Text = "C" + curJob.IndexPos.ToString("D4");
            btnCurCPos_Click(null, null);
        }

        //����һ��ʾ�̵��ڵ�ǰ��֮��
        private void buttonIns_Click(object sender, EventArgs e)
        {
            motion.teachJPos1 = motion.curJPos1;
            curJob.insPos(curJob.IndexPos + 1, motion.teachJPos1);
            curJob.IndexPos++;
            btnCurCPos.Text = "C" + curJob.IndexPos.ToString("D4");
        }

        //ɾ��һ��ʾ�̵�
        private void buttonDel_Click(object sender, EventArgs e)
        {
            if (curJob.IndexPos < 0)
                return;
            curJob.delPos(curJob.IndexPos);
            curJob.IndexPos--;
            if (curJob.IndexPos >= 0)
                btnCurCPos.Text = "C" + curJob.IndexPos.ToString("D4");
            else
                btnCurCPos.Text = "C----";
        }

        //������������� RC_ClrSts
        private void btnClr_Click(object sender, EventArgs e)
        {
            motion.RC_ClrSts(1, 8);
        }

        //�ָ��������ã���û�б����ڴ����ϣ�
        private void btnSetDefault_Click(object sender, EventArgs e)
        {
            if (motion.robotNum == 1)
            {
                motion.handTimeOpen1 = 0.5;
                motion.handTimeClose1 = 0.5;
                motion.handI1 = 5;
                motion.handO1 = 5;
                motion.tool1.X = 0;
                motion.tool1.Z = 0;

                motion.moveLimitN[0] = -2;
                motion.moveLimitP[0] = 2;
                motion.moveLimitN[1] = -2;
                motion.moveLimitP[1] = 2;
                motion.moveLimitN[2] = -2;
                motion.moveLimitP[2] = 2;
                motion.moveLimitN[3] = -4;
                motion.moveLimitP[3] = 4;

                motion.acc[0] = 1;
                motion.acc[1] = 1;
                motion.acc[2] = 1;
                motion.acc[3] = 1;
                motion.vmax[0] = 1;
                motion.vmax[1] = 1;
                motion.vmax[2] = 1;
                motion.vmax[3] = 1;
            }
            else
            {
                motion.handTimeOpen2 = 0.5;
                motion.handTimeClose2 = 0.5;
                motion.handI2 = 6;
                motion.handO2 = 6;
                motion.tool2.X = 0;
                motion.tool2.Z = 0;

                motion.moveLimitN[4] = -2;
                motion.moveLimitP[4] = 2;
                motion.moveLimitN[5] = -2;
                motion.moveLimitP[5] = 2;
                motion.moveLimitN[6] = -2;
                motion.moveLimitP[6] = 2;
                motion.moveLimitN[7] = -4;
                motion.moveLimitP[7] = 4;

                motion.acc[4] = 1;
                motion.acc[5] = 1;
                motion.acc[6] = 1;
                motion.acc[7] = 1;
                motion.vmax[4] = 1;
                motion.vmax[5] = 1;
                motion.vmax[6] = 1;
                motion.vmax[7] = 1;
            }

            NetIP = "192.168.0.2";
            NetPort = "6801";

            //������ʾ��ע�ⵥλ�任
            if (motion.robotNum == 1)
            {
                textBoxCatchTime.Text = (motion.handTimeClose1 * 1000).ToString();
                textBoxReleaseTime.Text = (motion.handTimeOpen1 * 1000).ToString();
                textBoxHandIN1.Text = motion.handI1.ToString();
                textBoxHandOUT1.Text = motion.handO1.ToString();
                textBoxHandX.Text = (motion.tool1.X * 1000).ToString();
                textBoxHandZ.Text = (motion.tool1.Z * 1000).ToString();

                textBoxLimitAP.Text = (motion.moveLimitP[0] * KAngle).ToString();
                textBoxLimitAN.Text = (motion.moveLimitN[0] * KAngle).ToString();
                textBoxLimitBP.Text = (motion.moveLimitP[1] * KAngle).ToString();
                textBoxLimitBN.Text = (motion.moveLimitN[1] * KAngle).ToString();
                textBoxLimitCP.Text = (motion.moveLimitP[2] * KAngle).ToString();
                textBoxLimitCN.Text = (motion.moveLimitN[2] * KAngle).ToString();
                textBoxLimitDP.Text = (motion.moveLimitP[3] * KAngle).ToString();
                textBoxLimitDN.Text = (motion.moveLimitN[3] * KAngle).ToString();
                textBoxVmaxA.Text = (motion.vmax[0] * KAngle).ToString();
                textBoxVmaxB.Text = (motion.vmax[1] * KAngle).ToString();
                textBoxVmaxC.Text = (motion.vmax[2] * KAngle).ToString();
                textBoxVmaxD.Text = (motion.vmax[3] * KAngle).ToString();
                textBoxAmaxA.Text = (motion.acc[0] * KAngle).ToString();
                textBoxAmaxB.Text = (motion.acc[1] * KAngle).ToString();
                textBoxAmaxC.Text = (motion.acc[2] * KAngle).ToString();
                textBoxAmaxD.Text = (motion.acc[3] * KAngle).ToString();
            }
            else
            {
                textBoxCatchTime.Text = (motion.handTimeClose2 * 1000).ToString();
                textBoxReleaseTime.Text = (motion.handTimeOpen2 * 1000).ToString();
                textBoxHandIN1.Text = motion.handI2.ToString();
                textBoxHandOUT1.Text = motion.handO2.ToString();
                textBoxHandX.Text = (motion.tool2.X * 1000).ToString();
                textBoxHandZ.Text = (motion.tool2.Z * 1000).ToString();

                textBoxLimitAP.Text = (motion.moveLimitP[4] * KAngle).ToString();
                textBoxLimitAN.Text = (motion.moveLimitN[4] * KAngle).ToString();
                textBoxLimitBP.Text = (motion.moveLimitP[5] * KAngle).ToString();
                textBoxLimitBN.Text = (motion.moveLimitN[5] * KAngle).ToString();
                textBoxLimitCP.Text = (motion.moveLimitP[6] * KAngle).ToString();
                textBoxLimitCN.Text = (motion.moveLimitN[6] * KAngle).ToString();
                textBoxLimitDP.Text = (motion.moveLimitP[7] * KAngle).ToString();
                textBoxLimitDN.Text = (motion.moveLimitN[7] * KAngle).ToString();
                textBoxVmaxA.Text = (motion.vmax[4] * KAngle).ToString();
                textBoxVmaxB.Text = (motion.vmax[5] * KAngle).ToString();
                textBoxVmaxC.Text = (motion.vmax[6] * KAngle).ToString();
                textBoxVmaxD.Text = (motion.vmax[7] * KAngle).ToString();
                textBoxAmaxA.Text = (motion.acc[4] * KAngle).ToString();
                textBoxAmaxB.Text = (motion.acc[5] * KAngle).ToString();
                textBoxAmaxC.Text = (motion.acc[6] * KAngle).ToString();
                textBoxAmaxD.Text = (motion.acc[7] * KAngle).ToString();
            }
            textBoxIP.Text = NetIP;
            textBoxPort.Text = NetPort;

            motion.RC_SetLimit(false);
        }

        //�������λ motion.RC_SetLimit  TB_releasestrict
        private void btnClrSoftLim_Click(object sender, EventArgs e)
        {
            if (!SYSPLC.TB_releasestrict)  //ԭ�����ޣ�������
            {
                if (motion.RC_SetLimit(true) == 0)
                    SYSPLC.TB_releasestrict = true;
            }
            else  //ԭ�����ޣ�������
            {
                if (motion.RC_SetLimit(false) == 0)
                    SYSPLC.TB_releasestrict = false;
            }
        }

        //��¼ʾ�̵�
        private void buttonAffirm_Click(object sender, EventArgs e)
        {
            if (curJob.IndexPos < 0)
                return;

            MathFun.JPOStyp curJPos = new MathFun.JPOStyp();
            if (motion.robotNum == 1)
                curJPos = motion.curJPos1;
            else if (motion.robotNum == 2)
                curJPos = motion.curJPos2; 
            if ((curJob.IndexPos < curJob.NumPos) && (curJob.IndexPos >= 0))
            {
                curJob.poses[curJob.IndexPos] = curJPos;  //������������
                curJob.delPos(curJob.IndexPos);  //�����ļ���¼����ɾ��ӣ�
                curJob.insPos(curJob.IndexPos, curJPos);
            }
        }

        //ǰ�����е�ʾ�̵� 
        private void buttonForward_Click(object sender, EventArgs e)
        {
            if (buttonServo.Text == "�ŷ�ʧ��")
            {
                if (buttonForward.Text == "ǰ��")
                {
                    MathFun.JPOStyp secJPos = new MathFun.JPOStyp();
                    secJPos = (MathFun.JPOStyp)curJob.poses[curJob.IndexPos];
                    MathFun.JPOStyp Vo = new MathFun.JPOStyp();
                    Vo.J1 = 0; Vo.J2 = 0; Vo.J3 = 0; Vo.J4 = 0;
                    if (motion.robotNum == 1)
                    {
                        MathFun.MF_MOVJ(motion.curJPos1, secJPos, Vo, motion.speedJ, 0);
                        motion.movType1 = Motion.MovType.PVT;
                        motion.moving1 = true;
                    }
                    else if (motion.robotNum == 2)
                    {
                        MathFun.MF_MOVJ(motion.curJPos2, secJPos, Vo, motion.speedJ, 0);
                        motion.movType2 = Motion.MovType.PVT;
                        motion.moving2 = true;
                    }
                    buttonForward.Text = "ֹͣ";
                }
                else
                {
                    buttonForward.Text = "ǰ��";
                    motion.RC_MovStop(false);  //ƽ��ֹͣ
                }
                if (labelSysInfo.Text == "��ʾ���������ŷ����˶�")
                    labelSysInfo.Text = "ϵͳ��ʾ��Ϣ";
            }
            else
            {
                labelSysInfo.Text = "��ʾ���������ŷ����˶�";
            }
        }

        #endregion

        //���н����¼�============================
        #region

        //�������ץȡ�߳�
        private void btnUse1_Click(object sender, EventArgs e)
        {
            if (btnUseD.Text == "ͣ��")  //���˫��Эͬģʽ����ֹ����
            {
                labelSysInfo.Text = "��ǰ����Э������ģʽ����ֹ���߳�������";
                return;
            }

            if (btnUse1.Text == "ͣ��")  //���ԭ�����У���ֹͣ
            {
                motion.run1 = false;
                if (motion.threadLine1 != null)  //����̴߳��ڣ���ɱ��
                {
                    motion.threadLine1.Abort();
                    motion.threadLine1 = null;
                }
                textLStep.Enabled = true;
                textLSpeed.Enabled = true;
                btnUse1.Text = "����";
                btnUse1.BackColor = Color.Green;
            }
            else//���ԭ��ֹͣ��������
            {
                //if (!SYSPLC.ServoR)
                //{
                //    MsgBox("����", "��ǰ�ŷ���Դδ��ͨ��");
                //    return;
                //}

                if (motion.threadLine1 != null)  //����̴߳��ڣ���ɱ�������½���������
                {
                    motion.threadLine1.Abort();
                    motion.threadLine1 = null;
                }
                motion.threadLine1 = new Thread(new ThreadStart(Line1));
                motion.threadLine1.IsBackground = true;
                motion.threadLine1.Start();

                textLStep.Enabled = false;
                textLSpeed.Enabled = false;
                btnUse1.Text = "ͣ��";
                btnUse1.BackColor = Color.DarkOrange;
                motion.run1 = true;
                motion.start1 = true;
            }
        }

        #endregion

        //�ܱ�ϵͳ��������ؼ���Ӧ===========================
        #region
        
        //����IO�˿ڵ�ע��
        private void btnsave_Click(object sender, EventArgs e)
        {
            Sv_Notation();//���洫����ע��
        }
        
        //�л��Ƿ�ˢ��IO��ʾ
        private void btnlock_Click(object sender, EventArgs e)
        {
            if (panelUserIO.Visible)
            {
                if (btnlock.Text == "����")
                {
                    IOrefreshLock = true;
                    btnlock.BackColor = System.Drawing.Color.Red;
                    btnlock.Text = "ȡ������";
                }
                else
                {
                    IOrefreshLock = false;
                    btnlock.BackColor = SystemColors.Control;
                    btnlock.Text = "����";
                }
            }
            if (panelSysIO.Visible)
            {
                if (btnlock1.Text == "����")
                {
                    IOrefreshLock = true;
                    btnlock1.BackColor = System.Drawing.Color.Red;
                    btnlock1.Text = "ȡ������";
                }
                else
                {
                    IOrefreshLock = false;
                    btnlock1.BackColor = SystemColors.Control;
                    btnlock1.Text = "����";
                }
            }
        }

        //ǿ�����
        private void btnenforce_Click(object sender, EventArgs e)
        {
            if (panelUserIO.Visible)
            {
                if (checkBoxO01.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0001;
                else SYSPLC.DOW = SYSPLC.DOW & 0xfffe;
                if (checkBoxO02.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0002;
                else SYSPLC.DOW = SYSPLC.DOW & 0xfffd;
                if (checkBoxO03.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0004;
                else SYSPLC.DOW = SYSPLC.DOW & 0xfffb;
                if (checkBoxO04.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0008;
                else SYSPLC.DOW = SYSPLC.DOW & 0xfff7;
                if (checkBoxO05.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0010;
                else SYSPLC.DOW = SYSPLC.DOW & 0xffef;
                if (checkBoxO06.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0020;
                else SYSPLC.DOW = SYSPLC.DOW & 0xffdf;
                if (checkBoxO07.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0040;
                else SYSPLC.DOW = SYSPLC.DOW & 0xffbf;
                if (checkBoxO08.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0080;
                else SYSPLC.DOW = SYSPLC.DOW & 0xff7f;
                if (checkBoxO09.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0100;
                else SYSPLC.DOW = SYSPLC.DOW & 0xfeff;
                if (checkBoxO10.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0200;
                else SYSPLC.DOW = SYSPLC.DOW & 0xfdff;
                if (checkBoxO11.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0400;
                else SYSPLC.DOW = SYSPLC.DOW & 0xfbff;
                if (checkBoxO12.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0800;
                else SYSPLC.DOW = SYSPLC.DOW & 0xf7ff;
                if (checkBoxO13.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x1000;
                else SYSPLC.DOW = SYSPLC.DOW & 0xefff;
                if (checkBoxO14.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x2000;
                else SYSPLC.DOW = SYSPLC.DOW & 0xdfff;
                if (checkBoxO15.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x4000;
                else SYSPLC.DOW = SYSPLC.DOW & 0xbfff;
                if (checkBoxO16.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x8000;
                else SYSPLC.DOW = SYSPLC.DOW & 0x7fff;
                if (btnlock.Text == "ȡ������")  //ǿ����ɺ��Զ�ȡ�������Ա�۲�Ч��
                {
                    IOrefreshLock = false;
                    btnlock.BackColor = SystemColors.Control;
                    btnlock.Text = "����";
                }
            }

            if (panelSysIO.Visible)
            {
                if (checkBoxC1.Checked) SYSPLC.Cls0 = true;
                else SYSPLC.Cls0 = false;
                if (checkBoxC2.Checked) SYSPLC.Cls1 = true;
                else SYSPLC.Cls1 = false;
                if (checkBoxC3.Checked) SYSPLC.Cls2 = true;
                else SYSPLC.Cls2 = false;
                if (checkBoxC4.Checked) SYSPLC.Cls3 = true;
                else SYSPLC.Cls3 = false;

                //if (checkBoxS1.Checked) motion.RC_ServoOn(1);//Gts.GT_AxisOn(1);
                //else Motion.RC_ServoOff(1);//Gts.GT_AxisOff(1);
                //if (checkBoxS2.Checked) motion.RC_ServoOn(2);//Gts.GT_AxisOn(2);
                //else Motion.RC_ServoOff(2);//Gts.GT_AxisOff(2);
                //if (checkBoxS3.Checked) motion.RC_ServoOn(3);//Gts.GT_AxisOn(3);
                //else Motion.RC_ServoOff(3);//Gts.GT_AxisOff(3);
                //if (checkBoxS4.Checked) motion.RC_ServoOn(4);//Gts.GT_AxisOn(4);
                //else Motion.RC_ServoOff(4);//Gts.GT_AxisOff(4);

                if (btnlock1.Text == "ȡ������")
                {
                    IOrefreshLock = false;
                    btnlock1.BackColor = SystemColors.Control;
                    btnlock1.Text = "����";
                }
            }

        }

        #endregion

        //ϵͳ��Ϣ����и�����ؼ���Ӧ=========================
        #region
        //panelCountInfo�а�ť�¼�
        private void btnClear_Click(object sender, EventArgs e)
        {
            CTtemp = 0;
            CT.Text = CTtemp.ToString();  //��ʾˢ��
        }

        //panelBackup�пؼ��¼�
        private void checkBoxAllFile_CheckStateChanged(object sender, EventArgs e)
        {
            if (checkBoxAllFile.Checked)
            {
                checkBoxJob.Checked = true;
                checkBoxParam.Checked = true;
                checkBoxErrList.Checked = true;
            }
        }
        private void checkBoxJob_CheckStateChanged(object sender, EventArgs e)
        {
            if (!checkBoxJob.Checked)
                checkBoxAllFile.Checked = false;
            if (checkBoxJob.Checked && checkBoxParam.Checked && checkBoxErrList.Checked)
                checkBoxAllFile.Checked = true;
        }

        private void checkBoxParam_CheckStateChanged(object sender, EventArgs e)
        {
            if (!checkBoxParam.Checked)
                checkBoxAllFile.Checked = false;
            if (checkBoxJob.Checked && checkBoxParam.Checked && checkBoxErrList.Checked)
                checkBoxAllFile.Checked = true;
        }

        private void checkBoxErrList_CheckStateChanged(object sender, EventArgs e)
        {
            if (!checkBoxErrList.Checked)
                checkBoxAllFile.Checked = false;
            if (checkBoxJob.Checked && checkBoxParam.Checked && checkBoxErrList.Checked)
                checkBoxAllFile.Checked = true;
        }
        private void btnStartSave_Click(object sender, EventArgs e)
        {
            string UPath = @"\USBDISK\";
            if (rbBackUp.Checked)
            {
                if (checkBoxJob.Checked)//���ݹ����ļ�
                {
                    string path1, path2;

                    try
                    {
                        path1 = Var.JobPath;
                        path2 = UPath;
                        string[] files = Directory.GetFiles(@path1);
                        foreach (string file in files)
                        {
                            FileInfo fi = new FileInfo(@file);
                            if (fi.Extension == ".JBI")
                            {
                                File.Copy(@file, path2 + Path.GetFileName(@file), true);
                            }
                        }
                    }
                    catch
                    {
                        MessageBox.Show("����Դ�����ļ��Ƿ����", "����");
                        return;
                    }
                    MessageBox.Show("�����ļ�����ɹ���");
                }

                if (checkBoxParam.Checked)
                {
                    string path1, path2;

                    try
                    {
                        path1 = Var.FilePath + "Robot.PRM";
                        path2 = UPath + "Robot.PRM";
                        File.Copy(path1, path2, true);

                    }
                    catch
                    {
                        MessageBox.Show("����Դϵͳ�����ļ��Ƿ����", "����");
                        return;
                    }
                    MessageBox.Show("ϵͳ�����ļ�����ɹ���");
                }

                if (checkBoxErrList.Checked)
                {
                    string path1, path2;

                    try
                    {
                        path1 = Var.FilePath + "Error.RCD";
                        path2 = UPath + "Error.RCD";
                        File.Copy(path1, path2, true);

                    }
                    catch
                    {
                        MessageBox.Show("����Դ������Ϣ�ļ��Ƿ����", "����");
                        return;
                    }
                    MessageBox.Show("������Ϣ�ļ�����ɹ���");
                }
            }
            else
            {
                if (rbRestore.Checked)//�ָ��ļ�
                {
                    if (checkBoxJob.Checked)
                    {
                        string path1, path2;

                        try
                        {
                            path1 = Var.JobPath;
                            path2 = UPath;
                            string[] files = Directory.GetFiles(@path2);
                            foreach (string file in files)
                            {
                                FileInfo fi = new FileInfo(@file);
                                if (fi.Extension == ".JBI")
                                {
                                    File.Copy(@file, path1 + Path.GetFileName(@file), true);
                                }
                            }
                        }
                        catch
                        {
                            MessageBox.Show("����Դ�����ļ��Ƿ����", "����");
                            return;
                        }

                        MessageBox.Show("�����ļ���ȡ�ɹ���");
                    }

                    if (checkBoxParam.Checked)
                    {
                        string path1, path2;

                        try
                        {
                            path1 = Var.FilePath + "Robot.PRM";
                            path2 = UPath + "Robot.PRM";
                            File.Copy(path2, path1, true);

                        }
                        catch
                        {
                            MessageBox.Show("����Դϵͳ�����ļ��Ƿ����", "����");
                            return;
                        }

                        MessageBox.Show("ϵͳ�����ļ���ȡ�ɹ���");
                    }

                    if (checkBoxErrList.Checked)
                    {
                        string path1, path2;

                        try
                        {
                            path1 = Var.FilePath + "Error.RCD";
                            path2 = UPath + "Error.RCD";
                            File.Copy(path2, path1, true);

                        }
                        catch
                        {
                            MessageBox.Show("����Դ������Ϣ�ļ��Ƿ����", "����");
                            return;
                        }

                        MessageBox.Show("������Ϣ�ļ���ȡ�ɹ���");
                    }
                }
            }
        }
        //�ļ�����ͻָ�·������
        private void listBox_file_SelectedIndexChanged(object sender, EventArgs e)
        {
            //try
            //{
            //    tbUpath.Text = listBox_file.SelectedItem.ToString() + "\\";
            //    DirectoryInfo dirInfo = new DirectoryInfo(listBox_file.SelectedItem.ToString());
            //    DirectoryInfo[] dirInfos = dirInfo.GetDirectories();
            //    listBox_file.Items.Clear();
            //    foreach (DirectoryInfo dir in dirInfos)
            //    {
            //        if (dir != null)
            //        {
            //            listBox_file.Items.Add(dir.FullName);
            //        }
            //    }
            //}
            //catch
            //{
            //    tbUpath.Text = "\\";
            //}
        }
        //�ļ�����ͻָ�·��
        private void tbUpath_GotFocus(object sender, EventArgs e)
        {
            //if (!listBox_file.Visible)
            //    listBox_file.Visible = true;
            //else
            //    listBox_file.Visible = false;

            //DirectoryInfo dirInfo = new DirectoryInfo("\\");
            //DirectoryInfo[] dirInfos = dirInfo.GetDirectories();
            //listBox_file.Items.Clear();
            //foreach (DirectoryInfo dir in dirInfos)
            //{
            //    if (dir != null)
            //    {
            //        listBox_file.Items.Add(dir.FullName);
            //    }
            //}
        }
        #endregion

        //�;���Timer�¼���1s��  DateTime.Now.ToLongDateString() + DateTime.Now.ToLongTimeString()
        private void timer1S_Tick(object sender, EventArgs e)
        {
            ontime++;  //ʱ���1��
            if (motion.moving1 || motion.moving2 || motion.movingD) runtime++;  //����ʱ���1��

            //ˢ��ͳ����Ϣ
            if (panelCountInfo.Visible)
            {
                labelTime.Text = DateTime.Now.ToLongDateString() + DateTime.Now.ToLongTimeString();  //��ʾ��ǰʱ��

                //�����ϵ��ʱ���֡���
                int hour = ontime / 3600;
                int min = (ontime % 3600) / 60;
                int sec = ontime % 60;
                OnTime.Text = hour.ToString() + "Сʱ" + min.ToString() + "��" + sec.ToString() + "��";

                //�������е�ʱ���֡���
                hour = runtime / 3600;
                min = (runtime % 3600) / 60;
                sec = runtime % 60;
                RunTime.Text = hour.ToString() + "Сʱ" + min.ToString() + "��" + sec.ToString() + "��";

                CTnow.Text = CTtoday.ToString();  //��ʾ���ο�����������
                CT.Text = CTtemp.ToString();  //��ʾ����
                CTall.Text = CThistory.ToString();  //��ʾ��ʷ����
            }

            //ˢ�¹��ϼ�¼����
            if (motion.errorMotion != 0)  //�˶�������������벻Ϊ�㣬δ��ʾ�����״γ��֣�
            {
                labelSysInfo.Text = Var.errMsgofMotion[motion.errorMotion] + "R" + motion.errorMotion.ToString("D4");
                if (errorCode < 30)  //��Ҫ����д���ļ�
                {
                    string Code = "R" + motion.errorMotion.ToString("D4");
                    string Time = DateTime.Now.ToLongDateString() + DateTime.Now.ToLongTimeString();  //��ǰʱ��
                    string Alarm = Var.errMsgofMotion[motion.errorMotion];
                    Sv_Alarm(Time, Code, Alarm);
                }
                motion.errorMotion = 0;
            }
            if (errorForm != 0)  //���漰���ϵͳ������벻Ϊ�㣬δ��ʾ�����״γ��֣�
            {
                labelSysInfo.Text = Var.errMsgofForm[errorForm] + "F" + errorForm.ToString("D4");
                if (errorCode < 30)  //��Ҫ����д���ļ�
                {
                    string Code = "F" + errorForm.ToString("D4");
                    string Time = DateTime.Now.ToLongDateString() + DateTime.Now.ToLongTimeString();  //��ǰʱ��
                    string Alarm = Var.errMsgofForm[errorForm];
                    Sv_Alarm(Time, Code, Alarm);
                }
                errorForm = 0;
            }
            if (errorMath != 0)  //���㺯���������벻Ϊ�㣬δ��ʾ�����״γ��֣�
            {
                labelSysInfo.Text = Var.errMsgofMath[errorMath] + "P" + errorMath.ToString("D4");
                if (errorCode < 30)  //��Ҫ����д���ļ�
                {
                    string Code = "P" + errorMath.ToString("D4");
                    string Time = DateTime.Now.ToLongDateString() + DateTime.Now.ToLongTimeString();  //��ǰʱ��
                    string Alarm = Var.errMsgofMath[errorMath];
                    Sv_Alarm(Time, Code, Alarm);
                }
                errorMath = 0;
            }
            if (motion.errorIO != 0)  //�ܱ�ϵͳ��IO��������벻Ϊ�㣬δ��ʾ�����״γ��֣�
            {
                labelSysInfo.Text = Var.errMsgofIO[motion.errorIO] + "IO" + motion.errorIO.ToString("D4");
                if (errorCode < 30)  //��Ҫ����д���ļ�
                {
                    string Code = "IO" + motion.errorIO.ToString("D4");
                    string Time = DateTime.Now.ToLongDateString() + DateTime.Now.ToLongTimeString();  //��ǰʱ��
                    string Alarm = Var.errMsgofIO[motion.errorIO];
                    Sv_Alarm(Time, Code, Alarm);
                }
                motion.errorIO = 0;
            }
            
            time1m++; time5s++;

            //ϵͳ��ʾ��Ϣ��λ����ʾ��Ϣ���ʾ5�룩
            if (time5s > 4)  
                if (labelSysInfo.Text != "ϵͳ��ʾ��Ϣ")
                {
                    labelSysInfo.Text = "ϵͳ��ʾ��Ϣ";
                    time5s = 0;
                }

            //ÿ10���ӱ���һ����Ϣ��ÿ�����Ƶ������
            if (time1m > 599)
            {
                time1m = 0;
                Sv_SysInfo();
            }
        }

        //�߾���Timer�¼���200ms�� �ѿ������� SYSPLC.IN
        private void timer200ms_Tick(object sender, EventArgs e)
        {
            if (SYSPLC.TB_play && !motion.teachLock && (currentMode != Var.CurrentMode.Play))
            {
                currentMode = Var.CurrentMode.Play;
                picOpMod.Image = Properties.Resources.AUTO;
                panelEncoder.Visible = false;
                panelManu.Visible = false;
                panelRun.Visible = true;
            }
            if (SYSPLC.TB_teach && !run && (currentMode != Var.CurrentMode.Teach))
            {
                currentMode = Var.CurrentMode.Teach;
                picOpMod.Image = Properties.Resources.HAND;

                panelEncoder.Visible = false;
                panelManu.Visible = true;
                panelRun.Visible = false;
            }
            if ((currentMode == Var.CurrentMode.Play) && SYSPLC.TB_start && !motion.start1)
            {
                if (motion.run1)
                    motion.start1 = true;
                else
                    btnUse1_Click(null, null);
                SYSPLC.TB_green = true;
                SYSPLC.TB_white = false;
            }
            if ((currentMode == Var.CurrentMode.Play) && SYSPLC.TB_hold && motion.start1)
            {
                motion.start1 = false;
                SYSPLC.TB_green = false;
                SYSPLC.TB_white = true;
            }

            if (panelManu.Visible && (tabControl.SelectedIndex == 2))  //�ֶ�������
            {
                //ʵʱˢ��
                if (motion.coord == MathFun.COORDtyp.Joint)//��ǰ����Ϊ�ؽ�����ʱ
                {
                    if (motion.robotNum == 1)
                    {
                        textBoxAR.Text = (motion.curJPos1.J1 * KAngle).ToString("F3") + "deg";
                        textBoxBR.Text = (motion.curJPos1.J2 * KAngle).ToString("F3") + "deg";
                        textBoxCR.Text = (motion.curJPos1.J3 * KAngle).ToString("F3") + "deg";
                        textBoxDR.Text = (motion.curJPos1.J4 * KAngle).ToString("F3") + "deg";
                    }
                    else
                    {
                        textBoxAR.Text = (motion.curJPos2.J1 * KAngle).ToString("F3") + "deg";
                        textBoxBR.Text = (motion.curJPos2.J2 * KAngle).ToString("F3") + "deg";
                        textBoxCR.Text = (motion.curJPos2.J3 * KAngle).ToString("F3") + "deg";
                        textBoxDR.Text = (motion.curJPos2.J4 * KAngle).ToString("F3") + "deg";
                    }
                }
                else  //�ѿ�������
                {
                    MathFun.HCOORDtyp tpPos = new MathFun.HCOORDtyp();
                    MathFun.JPOStyp jPos = new MathFun.JPOStyp();
                    double tp = 0;

                    if (motion.robotNum == 1)
                    {
                        jPos = motion.curJPos1;
                        tpPos = MathFun.MF_JOINT2ROBOT(motion.curJPos1);
                    }
                    else
                    {
                        jPos = motion.curJPos2;
                        tpPos = MathFun.MF_JOINT2ROBOT(motion.curJPos2);
                    }
                    tp = 1000 * tpPos.X;
                    textBoxAR.Text = tp.ToString("F2") + "mm";
                    tp = 1000 * tpPos.Z;
                    textBoxBR.Text = tp.ToString("F2") + "mm";
                    tp = (jPos.J1 + jPos.J2 + jPos.J3) * KAngle;
                    textBoxCR.Text = tp.ToString("F3") + "deg";
                    tp = jPos.J4 * KAngle;
                    textBoxDR.Text = tp.ToString("F3") + "deg";
                }
            }
            else if (panelRun.Visible && (tabControl.SelectedIndex == 2))  //�Զ�������
            {
                if (run)
                {
                    textLStep.Text = "?";
                    textRStep.Text = "?";
                    textDStep.Text = "?";
                }
            }
            else if (panelSysIO.Visible && (tabControl.SelectedIndex == 3))  //ϵͳIO������
            {
                //ˢ��IO�˿���ʾ
                if (!IOrefreshLock)  //ϵͳIO  
                {
                    int IO = 0;
                    int RLimit, LLimit, Home, Alarm, ClsR, ServR;

                    if (!SYSPLC.SCAN.WaitOne(5, false)) return;  //5ms���ܻ��PLC��Ϣ���˳� Mutex
                    RLimit = SYSPLC.RLimit;
                    LLimit = SYSPLC.LLimit;
                    Home = SYSPLC.Home;
                    Alarm = SYSPLC.Alarm;
                    ClsR = SYSPLC.ClsR;
                    ServR = SYSPLC.ServR;
                    SYSPLC.SCAN.ReleaseMutex();

                    IO = RLimit;
                    if ((IO & 0x01) == 0) checkBoxRL1.Checked = false;
                    else checkBoxRL1.Checked = true;
                    if ((IO & 0x02) == 0) checkBoxRL2.Checked = false;
                    else checkBoxRL2.Checked = true;
                    if ((IO & 0x04) == 0) checkBoxRL3.Checked = false;
                    else checkBoxRL3.Checked = true;
                    if ((IO & 0x08) == 0) checkBoxRL4.Checked = false;
                    else checkBoxRL4.Checked = true;
                    if ((IO & 0x10) == 0) checkBoxRL5.Checked = false;
                    else checkBoxRL5.Checked = true;
                    if ((IO & 0x20) == 0) checkBoxRL6.Checked = false;
                    else checkBoxRL6.Checked = true;
                    if ((IO & 0x40) == 0) checkBoxRL7.Checked = false;
                    else checkBoxRL7.Checked = true;
                    if ((IO & 0x80) == 0) checkBoxRL8.Checked = false;
                    else checkBoxRL8.Checked = true;

                    IO = LLimit;
                    if ((IO & 0x01) == 0) checkBoxLL1.Checked = false;
                    else checkBoxLL1.Checked = true;
                    if ((IO & 0x02) == 0) checkBoxLL2.Checked = false;
                    else checkBoxLL2.Checked = true;
                    if ((IO & 0x04) == 0) checkBoxLL3.Checked = false;
                    else checkBoxLL3.Checked = true;
                    if ((IO & 0x08) == 0) checkBoxLL4.Checked = false;
                    else checkBoxLL4.Checked = true;
                    if ((IO & 0x10) == 0) checkBoxLL5.Checked = false;
                    else checkBoxLL5.Checked = true;
                    if ((IO & 0x20) == 0) checkBoxLL6.Checked = false;
                    else checkBoxLL6.Checked = true;
                    if ((IO & 0x40) == 0) checkBoxLL7.Checked = false;
                    else checkBoxLL7.Checked = true;
                    if ((IO & 0x80) == 0) checkBoxLL8.Checked = false;
                    else checkBoxLL8.Checked = true;

                    IO = Home;
                    if ((IO & 0x01) == 0) checkBoxH1.Checked = false;
                    else checkBoxH1.Checked = true;
                    if ((IO & 0x02) == 0) checkBoxH2.Checked = false;
                    else checkBoxH2.Checked = true;
                    if ((IO & 0x04) == 0) checkBoxH3.Checked = false;
                    else checkBoxH3.Checked = true;
                    if ((IO & 0x08) == 0) checkBoxH4.Checked = false;
                    else checkBoxH4.Checked = true;
                    if ((IO & 0x10) == 0) checkBoxH5.Checked = false;
                    else checkBoxH5.Checked = true;
                    if ((IO & 0x20) == 0) checkBoxH6.Checked = false;
                    else checkBoxH6.Checked = true;
                    if ((IO & 0x40) == 0) checkBoxH7.Checked = false;
                    else checkBoxH7.Checked = true;
                    if ((IO & 0x80) == 0) checkBoxH8.Checked = false;
                    else checkBoxH8.Checked = true;

                    IO = Alarm;
                    if ((IO & 0x01) == 0) checkBoxA1.Checked = false;
                    else checkBoxA1.Checked = true;
                    if ((IO & 0x02) == 0) checkBoxA2.Checked = false;
                    else checkBoxA2.Checked = true;
                    if ((IO & 0x04) == 0) checkBoxA3.Checked = false;
                    else checkBoxA3.Checked = true;
                    if ((IO & 0x08) == 0) checkBoxA4.Checked = false;
                    else checkBoxA4.Checked = true;
                    if ((IO & 0x10) == 0) checkBoxA5.Checked = false;
                    else checkBoxA5.Checked = true;
                    if ((IO & 0x20) == 0) checkBoxA6.Checked = false;
                    else checkBoxA6.Checked = true;
                    if ((IO & 0x40) == 0) checkBoxA7.Checked = false;
                    else checkBoxA7.Checked = true;
                    if ((IO & 0x80) == 0) checkBoxA8.Checked = false;
                    else checkBoxA8.Checked = true;

                    IO = ClsR;
                    if ((IO & 0x01) == 0) checkBoxC1.Checked = false;
                    else checkBoxC1.Checked = true;
                    if ((IO & 0x02) == 0) checkBoxC2.Checked = false;
                    else checkBoxC2.Checked = true;
                    if ((IO & 0x04) == 0) checkBoxC3.Checked = false;
                    else checkBoxC3.Checked = true;
                    if ((IO & 0x08) == 0) checkBoxC4.Checked = false;
                    else checkBoxC4.Checked = true;
                    if ((IO & 0x10) == 0) checkBoxC5.Checked = false;
                    else checkBoxC5.Checked = true;
                    if ((IO & 0x20) == 0) checkBoxC6.Checked = false;
                    else checkBoxC6.Checked = true;
                    if ((IO & 0x40) == 0) checkBoxC7.Checked = false;
                    else checkBoxC7.Checked = true;
                    if ((IO & 0x80) == 0) checkBoxC8.Checked = false;
                    else checkBoxC8.Checked = true;

                    IO = ServR;
                    if ((IO & 0x01) == 0) checkBoxS1.Checked = false;
                    else checkBoxS1.Checked = true;
                    if ((IO & 0x02) == 0) checkBoxS2.Checked = false;
                    else checkBoxS2.Checked = true;
                    if ((IO & 0x04) == 0) checkBoxS3.Checked = false;
                    else checkBoxS3.Checked = true;
                    if ((IO & 0x08) == 0) checkBoxS4.Checked = false;
                    else checkBoxS4.Checked = true;
                    if ((IO & 0x10) == 0) checkBoxS5.Checked = false;
                    else checkBoxS5.Checked = true;
                    if ((IO & 0x20) == 0) checkBoxS6.Checked = false;
                    else checkBoxS6.Checked = true;
                    if ((IO & 0x40) == 0) checkBoxS7.Checked = false;
                    else checkBoxS7.Checked = true;
                    if ((IO & 0x80) == 0) checkBoxS8.Checked = false;
                    else checkBoxS8.Checked = true;
                }
            }
            else if (panelUserIO.Visible && (tabControl.SelectedIndex == 3))  //�û�IO������
            {
                //ˢ��IO�˿���ʾ
                if (!IOrefreshLock)  //ϵͳIO
                {
                    if (SYSPLC.IN[0]) checkBoxI01.Checked = true;
                    else checkBoxI01.Checked = false;
                    if (SYSPLC.IN[1]) checkBoxI02.Checked = true;
                    else checkBoxI02.Checked = false;
                    if (SYSPLC.IN[2]) checkBoxI03.Checked = true;
                    else checkBoxI03.Checked = false;
                    if (SYSPLC.IN[3]) checkBoxI04.Checked = true;
                    else checkBoxI04.Checked = false;
                    if (SYSPLC.IN[4]) checkBoxI05.Checked = true;
                    else checkBoxI05.Checked = false;
                    if (SYSPLC.IN[5]) checkBoxI06.Checked = true;
                    else checkBoxI06.Checked = false;
                    if (SYSPLC.IN[6]) checkBoxI07.Checked = true;
                    else checkBoxI07.Checked = false;
                    if (SYSPLC.IN[7]) checkBoxI08.Checked = true;
                    else checkBoxI08.Checked = false;
                    if (SYSPLC.IN[8]) checkBoxI09.Checked = true;
                    else checkBoxI09.Checked = false;
                    if (SYSPLC.IN[9]) checkBoxI10.Checked = true;
                    else checkBoxI10.Checked = false;
                    if (SYSPLC.IN[10]) checkBoxI11.Checked = true;
                    else checkBoxI11.Checked = false;
                    if (SYSPLC.IN[11]) checkBoxI12.Checked = true;
                    else checkBoxI12.Checked = false;
                    if (SYSPLC.IN[12]) checkBoxI13.Checked = true;
                    else checkBoxI13.Checked = false;
                    if (SYSPLC.IN[13]) checkBoxI14.Checked = true;
                    else checkBoxI14.Checked = false;
                    if (SYSPLC.IN[14]) checkBoxI15.Checked = true;
                    else checkBoxI15.Checked = false;
                    if (SYSPLC.IN[15]) checkBoxI16.Checked = true;
                    else checkBoxI16.Checked = false;

                    if (SYSPLC.OTR[0]) checkBoxO01.Checked = true;
                    else checkBoxO01.Checked = false;
                    if (SYSPLC.OTR[1]) checkBoxO02.Checked = true;
                    else checkBoxO02.Checked = false;
                    if (SYSPLC.OTR[2]) checkBoxO03.Checked = true;
                    else checkBoxO03.Checked = false;
                    if (SYSPLC.OTR[3]) checkBoxO04.Checked = true;
                    else checkBoxO04.Checked = false;
                    if (SYSPLC.OTR[4]) checkBoxO05.Checked = true;
                    else checkBoxO05.Checked = false;
                    if (SYSPLC.OTR[5]) checkBoxO06.Checked = true;
                    else checkBoxO06.Checked = false;
                    if (SYSPLC.OTR[6]) checkBoxO07.Checked = true;
                    else checkBoxO07.Checked = false;
                    if (SYSPLC.OTR[7]) checkBoxO08.Checked = true;
                    else checkBoxO08.Checked = false;
                    if (SYSPLC.OTR[8]) checkBoxO09.Checked = true;
                    else checkBoxO09.Checked = false;
                    if (SYSPLC.OTR[9]) checkBoxO10.Checked = true;
                    else checkBoxO10.Checked = false;
                    if (SYSPLC.OTR[10]) checkBoxO11.Checked = true;
                    else checkBoxO11.Checked = false;
                    if (SYSPLC.OTR[11]) checkBoxO12.Checked = true;
                    else checkBoxO12.Checked = false;
                    if (SYSPLC.OTR[12]) checkBoxO13.Checked = true;
                    else checkBoxO13.Checked = false;
                    if (SYSPLC.OTR[13]) checkBoxO14.Checked = true;
                    else checkBoxO14.Checked = false;
                    if (SYSPLC.OTR[14]) checkBoxO15.Checked = true;
                    else checkBoxO15.Checked = false;
                    if (SYSPLC.OTR[15]) checkBoxO16.Checked = true;
                    else checkBoxO16.Checked = false;
                }
            }
        }

        //�������µĴ��� //�ضϰ�����
        private void FormMain_KeyDown(object sender, KeyEventArgs e)
        {
            int keynum = (int)e.KeyData;
            bool cutkey = true;  //�Ƿ�ض�ϵͳ��������
            switch (keynum)
            {
                case 27:  //Esc
                    break;
                case 8:  //�˸�
                    break;
                case 113:  //F2������
                    SpeedUp();
                    break;
                case 114:  //F3������
                    SpeedDown();
                    break;
                case 115:  //F4��ѡ��
                    if ((tabControl.SelectedIndex == 0) && (curJob.JobName != null))
                    {
                        lbCmdPutin();
                        lbCmd.Visible = false;
                        SYSPLC.TB_informlist = false;
                    }
                    break;
                case 116:  //F5���ŷ�
                    buttonServo_Click(null, null);
                    break;
                case 118:  //F7��ȡ������
                    if (currentMode == Var.CurrentMode.Teach)  //ֻ��ʾ��״̬
                    {
                        if (!SYSPLC.TB_releasestrict)  //ԭ�����ޣ�������
                        {
                            if (motion.RC_SetLimit(true) == 0)
                                SYSPLC.TB_releasestrict = true;
                        }
                        else  //ԭ�����ޣ�������
                        {
                            if (motion.RC_SetLimit(false) == 0)
                                SYSPLC.TB_releasestrict = false;
                        }
                    }
                    break;
                case 119:  //F8����������
                    picManipulator_Click(null, null);
                    break;
                case 120:  //F9���ⲿ��
                    break;
                case 121:  //F10���໭��
                    break;
                case 86:  //V��ֱ�Ӵ�
                    break;
                case 71:  //G����ҳ
                    if (tabControl.SelectedIndex > 3)
                        tabControl.SelectedIndex = -1;
                    tabControl.SelectedIndex++;
                    break;
                case 46:  //Del������
                    break;
                case 13:  //�س�
                    if ((tabControl.SelectedIndex == 0) && (curJob.JobName != null))
                    {
                        lbCmdPutin();
                        lbCmd.Visible = false;
                        SYSPLC.TB_informlist = false;
                    }
                    break;
                case 70:  //F�����˵�
                    tabControl.Focus();
                    break;
                case 67:  //C������ϵ
                    picCoord_Click(null, null);
                    break;
                case 37:  //��
                case 38:  //��
                    if ((tabControl.SelectedIndex == 2) && panelManu.Visible)
                        btnPrev_Click(null, null);
                    else
                        cutkey = false;
                    break;
                case 39:  //��
                case 40:  //��
                    if ((tabControl.SelectedIndex == 2) && panelManu.Visible)
                        btnNext_Click(null, null);
                    else
                        cutkey = false;
                    break;
                case 81:  //Q��X-
                    if (SYSPLC.TB_teach && !motion.moving1 && !motion.moving2)
                        picAN_MouseDown(null, null);
                    break;
                case 87:  //W��X+
                    if (SYSPLC.TB_teach && !motion.moving1 && !motion.moving2)
                        picAP_MouseDown(null, null);
                    break;
                case 65:  //A��Y-
                    if (SYSPLC.TB_teach && !motion.moving1 && !motion.moving2)
                        picBN_MouseDown(null, null);
                    break;
                case 83:  //S��Y+
                    if (SYSPLC.TB_teach && !motion.moving1 && !motion.moving2)
                        picBP_MouseDown(null, null);
                    break;
                case 90:  //Z��Z-
                    if (SYSPLC.TB_teach && !motion.moving1 && !motion.moving2)
                        picCN_MouseDown(null, null);
                    break;
                case 88:  //X��Z+
                    if (SYSPLC.TB_teach && !motion.moving1 && !motion.moving2)
                        picCP_MouseDown(null, null);
                    break;
                case 85:  //U��A-
                    if (SYSPLC.TB_teach && !motion.moving1 && !motion.moving2)
                        picDN_MouseDown(null, null);
                    break;
                case 73:  //I��A+
                    if (SYSPLC.TB_teach && !motion.moving1 && !motion.moving2)
                        picDP_MouseDown(null, null);
                    break;
                case 74:  //J��B-
                    //motion.dirPos.THETA = -1;
                    //motion.RC_ManuMov();
                    break;
                case 75:  //K��B+
                    //motion.dirPos.THETA = 1;
                    //motion.RC_ManuMov();
                    break;
                case 78:  //N��C-
                    //motion.dirPos.PSI = -1;
                    //motion.RC_ManuMov();
                    break;
                case 77:  //M��C+
                    //motion.dirPos.PSI = 1;
                    //motion.RC_ManuMov();
                    break;
                case 65552:  //Shift���ϵ�
                    if (playMode == Var.PlayMode.Cycle)
                    {
                        playMode = Var.PlayMode.Auto;
                        picCycle.Image = Properties.Resources.AUTORUN;
                    }
                    else if (playMode == Var.PlayMode.Auto)
                    {
                        playMode = Var.PlayMode.Step;
                        picCycle.Image = Properties.Resources.POINTRUN;
                    }
                    else if (playMode == Var.PlayMode.Step)
                    {
                        playMode = Var.PlayMode.Cycle;
                        picCycle.Image = Properties.Resources.CYCLERUN;
                    }
                    break;
                case 131089:  //Ctrl������
                    if (currentMode == Var.CurrentMode.Teach)
                    {
                        if (motion.teachLock)
                        {
                            motion.teachLock = false;
                            picTeachLock.Image = Properties.Resources.TUNLOCK;
                        }
                        else
                        {
                            motion.teachLock = true;
                            picTeachLock.Image = Properties.Resources.TLOCK;
                        }
                    }
                    break;
                case 76:  //L���岹
                    break;
                case 82:  //R������
                    break;
                case 69:  //E������һ��
                    if ((tabControl.SelectedIndex == 0) && (curJob.JobName != null))
                    {
                        if (!lbCmd.Visible)
                        {
                            lbCmd.Visible = true;
                            lbCmd.SelectedIndex = 0;
                            lbCmd.Focus();
                            SYSPLC.TB_informlist = true;
                        }
                        else
                        {
                            lbCmd.Visible = false;
                            lbfile.Focus();
                            SYSPLC.TB_informlist = false;
                        }
                    }
                    break;
                case 79:  //O������
                    break;
                case 80:  //P��ǰ��
                    if ((tabControl.SelectedIndex == 2) && panelManu.Visible && (curJob.NumPos > 0) && !motion.moving1 && !motion.moving2)
                        buttonForward_Click(null, null);
                    break;
                case 66:  //B������
                    if (!SYSPLC.TB_insert)
                    {
                        SYSPLC.TB_insert = true;
                        SYSPLC.TB_modify = false;
                        SYSPLC.TB_delete = false;
                    }
                    else
                    {
                        SYSPLC.TB_insert = false;
                    }
                    break;
                case 68:  //D���޸�
                    if (!SYSPLC.TB_modify)
                    {
                        SYSPLC.TB_modify = true;
                        SYSPLC.TB_insert = false;
                        SYSPLC.TB_delete = false;
                    }
                    else
                    {
                        SYSPLC.TB_modify = false;
                    }
                    break;
                case 72:  //H�����
                    break;
                case 84:  //T��ɾ��
                    if (!SYSPLC.TB_delete)
                    {
                        SYSPLC.TB_delete = true;
                        SYSPLC.TB_insert = false;
                        SYSPLC.TB_modify = false;
                    }
                    else
                    {
                        SYSPLC.TB_delete = false;
                    }
                    break;
                case 89:  //Y��ȷ��
                    if ((tabControl.SelectedIndex == 0) && textCMD.Enabled)  //�ļ��༭ҳ��
                    {
                        if (SYSPLC.TB_modify)
                        {
                            picDelete_Click(null, null);
                            picInsert_Click(null, null);
                        }
                        else if (SYSPLC.TB_delete)
                            picDelete_Click(null, null);
                        else if (SYSPLC.TB_insert)
                            picInsert_Click(null, null);
                    }
                    else if ((tabControl.SelectedIndex == 2) && panelManu.Visible)  //�ֶ�ʾ��ҳ��
                    {
                        if (SYSPLC.TB_modify)
                            buttonAffirm_Click(null, null);
                        else if (SYSPLC.TB_delete)
                            buttonDel_Click(null, null);
                        else if (SYSPLC.TB_insert)
                            buttonIns_Click(null, null);
                    }

                    break;
                case 48:  //0
                    break;
                case 49:  //1
                    break;
                case 50:  //2
                    break;
                case 51:  //3
                    break;
                case 52:  //4
                    break;
                case 53:  //5
                    break;
                case 54:  //6
                    break;
                case 55:  //7
                    break;
                case 56:  //8
                    break;
                case 57:  //9
                    break;
                case 190:  //.
                    break;
                case 109:  //-
                    break;
                default:
                    break;
            }
            if (cutkey) e.Handled = true;
        }

        //����̧����
        private void FormMain_KeyUp(object sender, KeyEventArgs e)
        {
            switch ((int)e.KeyData)
            {
                case 81:  //Q��X-
                    picAN_MouseUp(null, null);
                    break;
                case 87:  //W��X+
                    picAP_MouseUp(null, null);
                    break;
                case 65:  //A��Y-
                    picBN_MouseUp(null, null);
                    break;
                case 83:  //S��Y+
                    picBP_MouseUp(null, null);
                    break;
                case 90:  //Z��Z-
                    picCN_MouseUp(null, null);
                    break;
                case 88:  //X��Z+
                    picCP_MouseUp(null, null);
                    break;
                case 85:  //U��A-
                    picDN_MouseUp(null, null);
                    break;
                case 73:  //I��A+
                    picDP_MouseUp(null, null);
                    break;
                case 74:  //J��B-
                    //motion.dirPos.THETA = 0;
                    //motion.RC_ManuMov();
                    break;
                case 75:  //K��B+
                    //motion.dirPos.THETA = 0;
                    //motion.RC_ManuMov();
                    break;
                case 78:  //N��C-
                    //motion.dirPos.PSI = 0;
                    //motion.RC_ManuMov();
                    break;
                case 77:  //M��C+
                    //motion.dirPos.PSI = 0;
                    //motion.RC_ManuMov();
                    break;
                case 80:  //P��ǰ��
                    if ((tabControl.SelectedIndex == 2) && panelManu.Visible && (curJob.NumPos > 0))
                        motion.RC_MovStop(false);
                    break;
                default:
                    break;
            }
        }

        private void panelEncoder_GotFocus(object sender, EventArgs e)
        {

        }




    
    }
}