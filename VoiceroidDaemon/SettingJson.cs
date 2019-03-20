using System.Runtime.Serialization;

namespace VoiceroidDaemon
{
    [DataContract]
    internal struct SettingJson
    {
        [DataMember]
        public Models.SystemSettingModel System;

        [DataMember]
        public Models.SpeakerSettingModel Speaker;
    }
}
