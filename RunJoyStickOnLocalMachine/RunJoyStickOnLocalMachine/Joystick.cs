using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.DirectX.DirectInput;
using System.Windows.Forms;
using System;

namespace RunJoyStickOnLocalMachine
{
    class Joystick
    {
        #region Local Variables

        private Device joystickDevice;
        private JoystickState state;
        public int Xaxis; // X-axis movement
        public int Yaxis; //Y-axis movement
        private IntPtr hWnd;
        public bool[] availableButtons;
        static bool[] joystickButtons;
        private string systemJoysticks;
        private static buttons currentState = buttons.NotRecognized,previousState = buttons.NotRecognized;
        const int axis_Max_Val = 65535; //Default max value
        #endregion

        //Map the values to enum
        public enum buttons
        {
            Green = 1,
            Red,
            Blue,
            Pink,
            L1,
            R1,
            L2,
            R2,
            Select,
            Start,
            Left,
            Right,
            Down,
            Up,
            NotRecognized,
        }

        /// <summary>
        /// Creates a new joystick object to the sent window
        /// </summary>
        /// <param name="window_handle">the " Handle " property of the desired window</param>
        public Joystick(IntPtr window_handle)
        {
            hWnd = window_handle;
            Xaxis = -1;
        }

        /// <summary>
        /// Finds tha avaiable joystick and returns its name
        /// </summary>
        /// <returns></returns>
        public string FindJoysticks()
        {
            systemJoysticks = null;

            try
            {
                // Find all the GameControl devices that are attached.
                DeviceList gameControllerList = Manager.GetDevices(DeviceClass.GameControl, EnumDevicesFlags.AttachedOnly);

                // check that we have at least one device.
                if (gameControllerList.Count > 0)
                {
                    foreach (DeviceInstance deviceInstance in gameControllerList)
                    {
                        // create a device from this controller so we can retrieve info.
                        joystickDevice = new Device(deviceInstance.InstanceGuid);
                        joystickDevice.SetCooperativeLevel(hWnd, CooperativeLevelFlags.Background | CooperativeLevelFlags.NonExclusive);

                        systemJoysticks = joystickDevice.DeviceInformation.InstanceName;

                        //Find it and exit
                        break;
                    }
                }
                else
                {
                    //Device not found
                    return null;
                }
            }
            catch
            {
                return null;
            }

            return systemJoysticks;
        }

        /// <summary>
        /// Get the joystick
        /// </summary>
        /// <param name="name">Joystick name returned from the FindJoysticks method</param>
        /// <returns></returns>
        public bool AcquireJoystick(string name)
        {
            try
            {
                //Get avaiable game controllers
                DeviceList gameControllerList = Manager.GetDevices(DeviceClass.GameControl, EnumDevicesFlags.AttachedOnly);
                bool found = false;

                //Find the first game controller available
                foreach (DeviceInstance deviceInstance in gameControllerList)
                {
                    if (deviceInstance.InstanceName == name)
                    {
                        found = true;
                        joystickDevice = new Device(deviceInstance.InstanceGuid);
                        joystickDevice.SetCooperativeLevel(hWnd, CooperativeLevelFlags.Background | CooperativeLevelFlags.NonExclusive);
                        break;
                    }
                }

                if (!found) //Joystick not found
                    return false;


                joystickDevice.SetDataFormat(DeviceDataFormat.Joystick);

                joystickDevice.Acquire();

                UpdateStatus();
            }
            catch (Exception)
            {
                return false;   //Failed to get the joystick
            }

            return true;
        }

        /// <summary>
        ///Release the joystick controller
        /// </summary>
        public void ReleaseJoystick()
        {
            joystickDevice.Unacquire();
        }

        /// <summary>
        /// Refresh the joystick states
        /// </summary>
        public void UpdateStatus()
        {
            Poll();
            //int[] extraAxis = state.GetSlider();    //Get joystick state

            Xaxis = state.X;
            Yaxis = state.Y;

            byte[] jsButtons = state.GetButtons();
            availableButtons = new bool[jsButtons.Length];

            int i = 0;
            foreach (byte button in jsButtons)
            {
                availableButtons[i] = button >= 128; //Loop on all buttons
                i++;
            }
        }

        private void Poll()
        {
            try
            {
                //Poll the joystick
                joystickDevice.Poll();

                //Update the joystick state field
                state = joystickDevice.CurrentJoystickState;
            }
            catch
            {
                throw (null);   //Failed to poll
            }
        }
        /// <summary>
        /// Find the attached joystick
        /// </summary>
        /// <param name="joystick">This will be the handles joystick object</param>
        /// <param name="joyStickTimer">JoystickTimer (Control) that ticks for the joystick</param>
        /// <returns></returns>
        public static bool connectToJoystick(Joystick joystick, Timer joyStickTimer)
        {
            while (true)
            {
                string sticks = joystick.FindJoysticks();

                if (sticks != null)
                {
                    if (joystick.AcquireJoystick(sticks))
                    {
                        joyStickTimer.Enabled = true;
                        break;  //Joystick found
                    }
                }
                else
                {
                    return false;   //Joystick not found
                }

            }
            return true;
        }

        /// <summary>
        /// Returns the joystick button, will be used for debouncing, check if the return is button.notRecognized
        /// </summary>
        /// <param name="js">Joystick object</param>
        /// <param name="jsTimer">JoystickTimer (Control) that ticks for the joystick</param>
        public static buttons getPressedButton(Joystick js, Timer jsTimer)
        {
            previousState = currentState;
           
            try
            {
                js.UpdateStatus();
                joystickButtons = js.availableButtons;   //Get joystick all buttons in a bool array

                if (js.Xaxis == 0)
                {
                    currentState = buttons.Left;
                }

                if (js.Xaxis == axis_Max_Val)
                {
                    currentState = buttons.Right;
                }

                if (js.Yaxis == 0)
                {
                    currentState = buttons.Up;
                }

                if (js.Yaxis == axis_Max_Val)
                {
                    currentState = buttons.Down;
                }

                //Find the pressed button
                for (int i = 0; i < joystickButtons.Length; i++)
                {
                    if (joystickButtons[i] == true) //The pressed button will have a true value
                    {
                        currentState = (buttons)(i + 1); //Save the pressed button
                    }
                }
            }
            catch
            {
                jsTimer.Enabled = false;    //Disable the timer
                Joystick.connectToJoystick(js, jsTimer);    //Reconnect to the joystick
            }
            return currentState;    //Return the current button
        }

        /// <summary>
        /// Checks if you released the button, Use this if you want the value to appear only once until it's changed
        /// </summary>
        /// <returns>True:the last button is released</returns>
        public static bool lastButtonRealeased()
        {
            if(currentState == buttons.NotRecognized && previousState != currentState)
            {
                return true;
            }
            return false;
        }
    }
}
