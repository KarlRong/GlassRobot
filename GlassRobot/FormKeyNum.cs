using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace GlassRobot
{
    public partial class FormKeyNum : Form
    {
        public FormKeyNum()
        {
            InitializeComponent();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            textBox1.Text += "1";
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            textBox1.Text += "2";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            textBox1.Text += "3";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            textBox1.Text += "4";
        }

        private void button5_Click(object sender, EventArgs e)
        {
            textBox1.Text += "5";
        }

        private void button6_Click(object sender, EventArgs e)
        {
            textBox1.Text += "6";
        }

        private void button7_Click(object sender, EventArgs e)
        {
            textBox1.Text += "7";
        }

        private void button8_Click(object sender, EventArgs e)
        {
            textBox1.Text += "8";
        }

        private void button9_Click(object sender, EventArgs e)
        {
            textBox1.Text += "9";
        }

        private void button0_Click(object sender, EventArgs e)
        {
            textBox1.Text += "0";
        }

        private void buttonDot_Click(object sender, EventArgs e)
        {
            textBox1.Text += ".";
        }

        private void buttonMinus_Click(object sender, EventArgs e)
        {
            textBox1.Text += "-";
        }

        private void buttonBack_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Length >= 1)
            {
                textBox1.Text = textBox1.Text.Remove(textBox1.Text.Length - 1, 1);
            }
            else
            {
                textBox1.Text = "";
            }
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            Close();
        }

        private void FormKeyNum_KeyPress(object sender, KeyPressEventArgs e)
        {
            int keynum = (int)e.KeyChar;
            switch (keynum)
            {
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
                case 46:  //.
                    break;
                case 45:  //-
                    break;
                case 37:  //左
                    break;
                case 38:  //上
                    break;
                case 39:  //右
                    break;
                case 40:  //下
                    break;
                case 8:  //退格
                    break;
                case 13:  //回车
                    buttonOk_Click(null, null);
                    break;
                default:
                    e.Handled = true;
                    break;
            }
        }

        private void textBox1_GotFocus(object sender, EventArgs e)
        {
            textBox1.SelectionStart = textBox1.Text.Length;
        }
    }
}