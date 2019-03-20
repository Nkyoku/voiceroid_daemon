using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VoiceroidDaemon.Models;
using Aitalk;

namespace VoiceroidDaemon.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult SystemSetting()
        {
            // 言語のリストを作成してビューに渡す
            ViewData["LanguageListItems"] = GenerateSelectListItems(AitalkWrapper.LanguageList);
            return View(Setting.System);
        }

        [HttpPost]
        public IActionResult SystemSetting(SystemSettingModel system_setting)
        {
            if (system_setting != null)
            {
                string error_message = null;
                bool saved = false;
                Setting.Lock();
                try
                {
                    error_message = Setting.ApplySystemSetting(system_setting);
                    saved = Setting.Save();
                }
                finally
                {
                    Setting.Unlock();
                }
                if (saved == false)
                {
                    ViewData["Alert"] = "設定の保存に失敗しました。";
                }
                else if (error_message != null)
                {
                    ViewData["Alert"] = $"設定は保存されましたがエラーが発生しました。{error_message}";
                }
                else
                {
                    ViewData["Alert"] = "設定は保存され、設定の有効性が確認されました。";
                }
            }
            return SystemSetting();
        }
        
        [HttpGet]
        public IActionResult SpeakerSetting(string voice_db)
        {
            SpeakerSettingModel model = Setting.Speaker.Clone();
            model.VoiceDbName = voice_db ?? Setting.Speaker.VoiceDbName;
            
            // 話者名のリストを取得する
            string[] voice_names = null;
            Setting.Lock();
            try
            {
                if ((model.VoiceDbName != null) && (0 < model.VoiceDbName.Length))
                {
                    AitalkWrapper.LoadVoice(model.VoiceDbName);
                    voice_names = AitalkWrapper.Parameter.VoiceNames;
                }
            }
            catch (Exception) { }
            finally
            {
                Setting.Unlock();
            }
            
            // 音声ライブラリと話者のリストを作成してビューに渡す
            ViewData["VoiceDbListItems"] = GenerateSelectListItems(AitalkWrapper.VoiceDbList);
            ViewData["SpeakerListItems"] = GenerateSelectListItems(voice_names);
            return View(model);
        }

        [HttpPost]
        public IActionResult SpeakerSetting(SpeakerSettingModel speaker_setting)
        {
            if (speaker_setting != null)
            {
                string error_message = null;
                bool saved = false;
                Setting.Lock();
                try
                {
                    error_message = Setting.ApplySpeakerSetting(speaker_setting);
                    saved = Setting.Save();
                }
                finally
                {
                    Setting.Unlock();
                }
                if (saved == false)
                {
                    ViewData["Alert"] = "設定の保存に失敗しました。";
                }
                else if (error_message != null)
                {
                    ViewData["Alert"] = $"設定は保存されましたがエラーが発生しました。{error_message}";
                }
                else
                {
                    ViewData["Alert"] = "設定は保存され、設定の有効性が確認されました。";
                }
            }
            return SpeakerSetting((string)null);
        }
        
        [HttpGet("/favicon.png")]
        public IActionResult Favicon()
        {
            if (Setting.IconByteArray != null)
            {
                return new FileContentResult(Setting.IconByteArray, "image/png");
            }
            else
            {
                return new NotFoundResult();
            }
        }
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        
        private static List<SelectListItem> GenerateSelectListItems(string[] values)
        {
            // 選択項目リストを作成する
            // 0番目に値がnullなDefault項目を追加する
            List<SelectListItem> result = new List<SelectListItem>();
            result.Add(new SelectListItem("Default", ""));
            if (values != null)
            {
                result.AddRange(values.Select(name => new SelectListItem(name, name)));
            }
            return result;
        }
    }
}
