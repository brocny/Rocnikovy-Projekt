﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace FsdkFaceLib.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.7.0.0")]
    public sealed partial class FsdkSettings : global::System.Configuration.ApplicationSettingsBase {
        
        private static FsdkSettings defaultInstance = ((FsdkSettings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new FsdkSettings())));
        
        public static FsdkSettings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("100")]
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
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool FsdkHandleArbitraryRot {
            get {
                return ((bool)(this["FsdkHandleArbitraryRot"]));
            }
            set {
                this["FsdkHandleArbitraryRot"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
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
        [global::System.Configuration.DefaultSettingValueAttribute("8")]
        public int SkipMinimumConfirmations {
            get {
                return ((int)(this["SkipMinimumConfirmations"]));
            }
            set {
                this["SkipMinimumConfirmations"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("12")]
        public int MaxSkippedFrames {
            get {
                return ((int)(this["MaxSkippedFrames"]));
            }
            set {
                this["MaxSkippedFrames"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public int PipelineParallelism {
            get {
                return ((int)(this["PipelineParallelism"]));
            }
            set {
                this["PipelineParallelism"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2")]
        public int PipelineQueueDepth {
            get {
                return ((int)(this["PipelineQueueDepth"]));
            }
            set {
                this["PipelineQueueDepth"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool FsdkDetectFeatures {
            get {
                return ((bool)(this["FsdkDetectFeatures"]));
            }
            set {
                this["FsdkDetectFeatures"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool FsdkDetectFace {
            get {
                return ((bool)(this["FsdkDetectFace"]));
            }
            set {
                this["FsdkDetectFace"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool FsdkDetectExpression {
            get {
                return ((bool)(this["FsdkDetectExpression"]));
            }
            set {
                this["FsdkDetectExpression"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("qVyhlxKiB1+NfuKH8HjrN2QA5oaMXI/aYiH9AS9oDDTjdGZ9fwODv9iTJG/rAow2uRnGFkvZnjVbCHWSK" +
            "ThqoetUYxlMAI1j+Gm9sjl8eEi7zN01FgwY/bK7R6+NrSZ1aOMiWPP1NG8JcrzgIfH9qNCTI+9kQkw6N" +
            "s/mHES7O2E=")]
        public string FsdkActiovationKey {
            get {
                return ((string)(this["FsdkActiovationKey"]));
            }
            set {
                this["FsdkActiovationKey"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.998")]
        public float InstantMatchThreshold {
            get {
                return ((float)(this["InstantMatchThreshold"]));
            }
            set {
                this["InstantMatchThreshold"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.992")]
        public float NewTemplateThreshold {
            get {
                return ((float)(this["NewTemplateThreshold"]));
            }
            set {
                this["NewTemplateThreshold"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.987")]
        public float MatchThreshold {
            get {
                return ((float)(this["MatchThreshold"]));
            }
            set {
                this["MatchThreshold"] = value;
            }
        }
    }
}
