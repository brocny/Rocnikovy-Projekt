using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;

namespace App.KinectTracked
{
    internal class DialogState
    {
        public CommonDialog Dialog;
        public DialogResult Result;

        public void ThreadProcShowDialog()
        {
            Result = Dialog.ShowDialog();
        }
    }

    internal static class DialogHelpers
    {
        internal static DialogResult STAShowDialog(this CommonDialog dialog)
        {
            var state = new DialogState {Dialog = dialog};
            var thread = new Thread(state.ThreadProcShowDialog);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            return state.Result;
        }
    }

    public static class ControlHelpers
    {
        public static void InvokeIfRequired<T>(this T control, Action<T> action) where T : ISynchronizeInvoke
        {
            if (control.InvokeRequired)
            {
                control.BeginInvoke(new Action(() => action(control)), null);
            }
            else
            {
                action(control);
            }
        }
    }
}