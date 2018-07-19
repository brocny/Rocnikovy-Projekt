using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FsdkFaceLib;
using FsdkFaceLib.Properties;

namespace App
{
    class FsdkInitializeHelper
    {
        public static void InitializeLibrary()
        {
            while (true)
            {
                try
                {
                    FSDKFacePipeline.InitializeLibrary();
                    return;
                }
                catch (ApplicationException e)
                {
                    var result = MessageBox.Show(e.Message + Environment.NewLine + "Do you wish to enter a new key?", "Face library activation failed!", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        var inputKeyForm = new InputNameForm("FaceSDK Key", "Enter new FSDK key");
                        result = inputKeyForm.ShowDialog();
                        if (result == DialogResult.OK)
                        {
                            FsdkSettings.Default.FsdkActivationKey = inputKeyForm.UserName;
                            FsdkSettings.Default.Save();
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }
    }
}
