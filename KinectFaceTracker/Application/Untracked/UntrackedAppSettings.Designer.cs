﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace App.Untracked {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.7.0.0")]
    internal sealed partial class UntrackedAppSettings : global::System.Configuration.ApplicationSettingsBase {
        
        private static UntrackedAppSettings defaultInstance = ((UntrackedAppSettings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new UntrackedAppSettings())));
        
        public static UntrackedAppSettings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("400")]
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
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool EnableAutoLearning {
            get {
                return ((bool)(this["EnableAutoLearning"]));
            }
            set {
                this["EnableAutoLearning"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool FsdkDetermineRotationAngle {
            get {
                return ((bool)(this["FsdkDetermineRotationAngle"]));
            }
            set {
                this["FsdkDetermineRotationAngle"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool FsdkHandleArbitraryRotations {
            get {
                return ((bool)(this["FsdkHandleArbitraryRotations"]));
            }
            set {
                this["FsdkHandleArbitraryRotations"] = value;
            }
        }
    }
}