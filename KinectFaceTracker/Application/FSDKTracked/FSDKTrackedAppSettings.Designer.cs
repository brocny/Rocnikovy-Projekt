﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace App.FSDKTracked {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.6.0.0")]
    internal sealed partial class FSDKTrackedAppSettings : global::System.Configuration.ApplicationSettingsBase {
        
        private static FSDKTrackedAppSettings defaultInstance = ((FSDKTrackedAppSettings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new FSDKTrackedAppSettings())));
        
        public static FSDKTrackedAppSettings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("540")]
        public int FsdkInternalResizeWidth {
            get {
                return ((int)(this["FsdkInternalResizeWidth"]));
            }
            set {
                this["FsdkInternalResizeWidth"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3")]
        public int FsdkFaceDetectionThreshold {
            get {
                return ((int)(this["FsdkFaceDetectionThreshold"]));
            }
            set {
                this["FsdkFaceDetectionThreshold"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool FsdkDetermineRotAngle {
            get {
                return ((bool)(this["FsdkDetermineRotAngle"]));
            }
            set {
                this["FsdkDetermineRotAngle"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool FsdkHandleArbitraryRot {
            get {
                return ((bool)(this["FsdkHandleArbitraryRot"]));
            }
            set {
                this["FsdkHandleArbitraryRot"] = value;
            }
        }
    }
}