#pragma once
#include <string>
#include <vector>
#include <Windows.h>
#include "aitalk_AITalk.h"

using namespace System;
using namespace Runtime::InteropServices;

namespace AITalkWrapper {
    public ref class AITalkWrapper {
    public:
        // 音声データベースのサンプルレート [Hz]
        literal int VoiceDbSampleRate = 44100;

        // 標準のタイムアウト [ms]
        literal int DefaultTimeOut = 10000;

        // デストラクタ
        ~AITalkWrapper();

        // エラー文字列を取得する
        String^ GetLastError(void);

        // AITalkライブラリを開く
        bool OpenLibrary(String ^install_path, String ^auth_code_seed, int timeout);

        // AITalkライブラリを閉じる
        void CloseLibrary(void);

        // AITalkライブラリが正常に開けたか取得する
        bool IsLibraryOpened(void);
        
        // 言語ファイルを読み込む
        bool LoadLanguage(String ^language_name);

        // フレーズ辞書を読み込む
        bool LoadPhraseDictionary(String ^path);

        // 単語辞書を読み込む
        bool LoadWordDictionary(String ^path);

        // 記号ポーズ辞書を読み込む
        bool LoadSymbolDictionary(String ^path);

        // ボイスライブラリを読み込む
        bool LoadVoice(String ^voice_name);

        // 文章を仮名に変換する
        String^ TextToKana(String ^text, int timeout);

        // 仮名を音声に変換する
        array<Byte>^ KanaToSpeech(String ^kana, int timeout);

        // 仮名を音声に変換する
        // 変換イベントも受け取る
        array<Byte>^ KanaToSpeech(String ^kana, int timeout, [Out] array<Tuple<UInt64, String^>^> ^%event);

    private:
        // ユニコード文字列をShift-JISに変換し、Shift-JISの各バイトがユニコード文字列の何文字目に対応するかを返す
        bool UnicodeToShiftJIS(const std::wstring &unicode_string, std::string *ascii_string, std::vector<int> *ascii_to_unicode);

        // このラッパーライブラリを初期化する
        void InitializeAll(void);

        // エラー文字列を消去する
        void ClearLastError(void);

        // エラー文字列を設定する
        void SetLastError(String ^text);

        // エラー文字列
        String ^ErrorString = L"";

        // VOICEROID2のインストールディレクトリ
        String ^InstallDirectory = L"";

        // aitalked.dllのモジュールハンドル
        HMODULE ModuleHandle = NULL;

        // aitalked.dllの関数
        AITalkResultCode(__stdcall *AITalkAPI_CloseKana)(int32_t, int32_t) = nullptr;
        AITalkResultCode(__stdcall *AITalkAPI_CloseSpeech)(int32_t, int32_t) = nullptr;
        AITalkResultCode(__stdcall *AITalkAPI_End)(void) = nullptr;
        AITalkResultCode(__stdcall *AITalkAPI_GetData)(int32_t, int16_t*, uint32_t, uint32_t*) = nullptr;
        AITalkResultCode(__stdcall *AITalkAPI_GetKana)(int32_t, char*, uint32_t, uint32_t*, uint32_t*) = nullptr;
        AITalkResultCode(__stdcall *AITalkAPI_GetParam)(AITalk_TTtsParam*, uint32_t*) = nullptr;
        AITalkResultCode(__stdcall *AITalkAPI_Init)(AITalk_TConfig*) = nullptr;
        AITalkResultCode(__stdcall *AITalkAPI_LangClear)(void) = nullptr;
        AITalkResultCode(__stdcall *AITalkAPI_LangLoad)(const char*) = nullptr;
        AITalkResultCode(__stdcall *AITalkAPI_LicenseDate)(char*) = nullptr;
        AITalkResultCode(__stdcall *AITalkAPI_LicenseInfo)(const char*, char*, uint32_t, uint32_t*) = nullptr;
        AITalkResultCode(__stdcall *AITalkAPI_ReloadPhraseDic)(const char*) = nullptr;
        AITalkResultCode(__stdcall *AITalkAPI_ReloadSymbolDic)(const char*) = nullptr;
        AITalkResultCode(__stdcall *AITalkAPI_ReloadWordDic)(const char*) = nullptr;
        AITalkResultCode(__stdcall *AITalkAPI_SetParam)(const AITalk_TTtsParam*) = nullptr;
        AITalkResultCode(__stdcall *AITalkAPI_TextToKana)(int32_t*, AITalk_TJobParam*, const char*) = nullptr;
        AITalkResultCode(__stdcall *AITalkAPI_TextToSpeech)(int32_t*, AITalk_TJobParam*, const char*) = nullptr;
        AITalkResultCode(__stdcall *AITalkAPI_VersionInfo)(int32_t, char*, uint32_t, uint32_t*) = nullptr;
        AITalkResultCode(__stdcall *AITalkAPI_VoiceClear)(void) = nullptr;
        AITalkResultCode(__stdcall *AITalkAPI_VoiceLoad)(const char*) = nullptr;
    };
}
