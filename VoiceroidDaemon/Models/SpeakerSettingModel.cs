using System;
using System.Runtime.Serialization;

namespace VoiceroidDaemon.Models
{
    [DataContract]
    public class SpeakerSettingModel
    {
        /// <summary>
        /// ボイスライブラリ名
        /// </summary>
        [DataMember]
        public string VoiceDbName { get; set; }

        /// <summary>
        /// 話者名。単一話者のライブラリならボイスライブラリ名と同じだと思われる。
        /// </summary>
        [DataMember]
        public string SpeakerName { get; set; }
        
        /// <summary>
        /// デシリアライズ前に呼ばれる。
        /// 初期値を代入する。
        /// </summary>
        /// <param name="context"></param>
        [OnDeserializing]
        internal void OnDeserializing(StreamingContext context)
        {
            LoadInitialValues();
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SpeakerSettingModel()
        {
            LoadInitialValues();
        }

        /// <summary>
        /// メンバーに初期値を代入する
        /// </summary>
        private void LoadInitialValues()
        {
            VoiceDbName = "";
            SpeakerName = "";
        }

        /// <summary>
        /// クローンを作成する
        /// </summary>
        /// <returns></returns>
        public SpeakerSettingModel Clone()
        {
            return (SpeakerSettingModel)MemberwiseClone();
        }
    }
}
