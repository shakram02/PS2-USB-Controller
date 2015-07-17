
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Management;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;

namespace RunJoyStickOnLocalMachine
{

    public partial class Form1 : Form
    {
        private Joystick joystick;  //joystick object

        //Create empty joystick buttons
        Joystick.buttons currentButton = Joystick.buttons.NotRecognized, previousButton = Joystick.buttons.NotRecognized;

        public Form1()
        {
            InitializeComponent();


            joystick = new Joystick(this.Handle);

            if (Joystick.connectToJoystick(joystick, joystickTimer) == false)
            {
                MessageBox.Show("Device Not found!");
                this.Dispose(); //Free resources
                this.Close();   //Terminate the program
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Joystick timer event handler
        /// </summary>
        /// <param name="sender">Joystick timer</param>
        /// <param name="e">Event arguments</param>
        private void joystickTimer_Tick_1(object sender, EventArgs e)
        {
            currentButton = Joystick.getPressedButton(joystick, (System.Windows.Forms.Timer)sender);

            //Debounce when all the buttons are released, if you want the robot to move you'll keep you hands on the button
            //When you relase the button, a stop command will be given to the robot,
            //then you don't need to read anything if all the buttons are released.

            if (currentButton != Joystick.buttons.NotRecognized && currentButton != previousButton)
            {
                output.Text += currentButton.ToString() + "\n";
                autoScroll();
            }
            else if (Joystick.lastButtonRealeased())
            {
                output.Text += currentButton.ToString() + "\n";
                autoScroll();
            }
            previousButton = currentButton;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            output.Text = "";   //Clear the text
        }

        void autoScroll()
        {
            output.SelectionStart = output.Text.Length;
            output.ScrollToCaret();
        }
    }
}