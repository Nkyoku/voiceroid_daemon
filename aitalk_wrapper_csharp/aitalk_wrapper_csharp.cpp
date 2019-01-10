// C#からAITalkを呼びやすくするためのラッパーライブラリ

#include "aitalk_wrapper_csharp.h"
#include <msclr/marshal.h>

using namespace msclr::interop;

// モジュールが読み込まれていない時に出力するエラーコード
static const wchar_t ERRORMESSAGE_MODULE_NOT_LOADED[] = L"モジュールが読み込まれていません。";

// 仮名変換の情報を保持する構造体
struct KanaConverter_t {
    // バッファサイズ [bytes]
    static const uint32_t BufferSize = 0x1000;

    // バッファ
    std::vector<char> Buffer;

    // 出力先
    std::string Output;

    // 同期のためのイベントハンドル
    HANDLE CloseEventHandle = NULL;

    // 取得関数
    AITalkResultCode(__stdcall *GetKana)(int32_t, char*, uint32_t, uint32_t*, uint32_t*) = nullptr;

    // コンストラクタ
    KanaConverter_t() : Buffer(BufferSize) {
        CloseEventHandle = CreateEvent(NULL, FALSE, FALSE, NULL);
    }

    // デストラクタ
    ~KanaConverter_t() {
        CloseHandle(CloseEventHandle);
    }
};

// 音声変換の情報を保持する構造体
struct SpeechConverter_t {
    // バッファサイズ [samples]
    static const uint32_t BufferSize = 0x10000;
    
    // バッファ
    std::vector<uint8_t> Buffer;

    // 出力先
    std::vector<uint8_t> Output;

    // イベントの出力先
    std::vector<std::pair<uint64_t, std::string>> EventOutput;

    // 同期のためのイベントハンドル
    HANDLE CloseEventHandle = NULL;

    // データ取得関数
    AITalkResultCode(__stdcall *GetData)(int32_t, int16_t*, uint32_t, uint32_t*) = nullptr;

    // コンストラクタ
    SpeechConverter_t() : Buffer(BufferSize) {
        CloseEventHandle = CreateEvent(NULL, FALSE, FALSE, NULL);
    }

    // デストラクタ
    ~SpeechConverter_t() {
        CloseHandle(CloseEventHandle);
    }
};

// 仮名変換の際のコールバック関数
static int __stdcall CallbackTextBuf(AITalkEventReasonCode reason_code, int32_t job_id, void *user_data);

// 音声変換の際のコールバック関数
static int __stdcall CallbackRawBuf(AITalkEventReasonCode reason_code, int32_t job_id, uint64_t tick, void *user_data);

// 音声変換の際にイベントが返されるコールバック関数
static int __stdcall CallbackEventTts(AITalkEventReasonCode reason_code, int32_t job_id, uint64_t tick, const char *name, void *user_data);

// モジュールから関数をロードする
template<typename T> static bool getFunction(HMODULE module_handle, const char *function_name, T *%function) {
    FARPROC fp = GetProcAddress(module_handle, function_name);
    function = reinterpret_cast<T*>(fp);
    return (fp != nullptr);
}

AITalkWrapper::AITalkWrapper::~AITalkWrapper(void) {
    CloseLibrary();
}

String^ AITalkWrapper::AITalkWrapper::GetLastError(void) {
    return ErrorString;
}

void AITalkWrapper::AITalkWrapper::ClearLastError(void) {
    ErrorString = L"";
}

void AITalkWrapper::AITalkWrapper::SetLastError(String ^text) {
    ErrorString = text;
}

void AITalkWrapper::AITalkWrapper::InitializeAll(void) {
    ClearLastError();
    InstallDirectory = L"";
    if (ModuleHandle != nullptr) {
        FreeLibrary(ModuleHandle);
        ModuleHandle = nullptr;
    }
    AITalkAPI_CloseKana = nullptr;
    AITalkAPI_CloseSpeech = nullptr;
    AITalkAPI_End = nullptr;
    AITalkAPI_GetData = nullptr;
    AITalkAPI_GetKana = nullptr;
    AITalkAPI_GetParam = nullptr;
    AITalkAPI_Init = nullptr;
    AITalkAPI_LangClear = nullptr;
    AITalkAPI_LangLoad = nullptr;
    AITalkAPI_LicenseDate = nullptr;
    AITalkAPI_LicenseInfo = nullptr;
    AITalkAPI_ReloadPhraseDic = nullptr;
    AITalkAPI_ReloadSymbolDic = nullptr;
    AITalkAPI_ReloadWordDic = nullptr;
    AITalkAPI_SetParam = nullptr;
    AITalkAPI_TextToKana = nullptr;
    AITalkAPI_TextToSpeech = nullptr;
    AITalkAPI_VersionInfo = nullptr;
    AITalkAPI_VoiceClear = nullptr;
    AITalkAPI_VoiceLoad = nullptr;
}

bool AITalkWrapper::AITalkWrapper::OpenLibrary(String ^install_path, String ^auth_code_seed, int timeout) {
    marshal_context context;
    
    // 開く前に閉じておく
    CloseLibrary();

    InstallDirectory = install_path;

    // DLLをロードする
    String ^module_path_managed = InstallDirectory + L"\\aitalked.dll";
    std::string module_path = context.marshal_as<const char*>(module_path_managed);
    ModuleHandle = LoadLibraryA(module_path.c_str());
    if (ModuleHandle == nullptr) {
        InitializeAll();
        SetLastError(String::Format(L"モジュール'{0}'の読み込みに失敗しました。", module_path_managed));
        return false;
    }

    // 関数ポインタを取得する
    bool ok = true;
    ok &= getFunction(ModuleHandle, "_AITalkAPI_CloseKana@8", AITalkAPI_CloseKana);
    ok &= getFunction(ModuleHandle, "_AITalkAPI_CloseSpeech@8", AITalkAPI_CloseSpeech);
    ok &= getFunction(ModuleHandle, "_AITalkAPI_End@0", AITalkAPI_End);
    ok &= getFunction(ModuleHandle, "_AITalkAPI_GetData@16", AITalkAPI_GetData);
    ok &= getFunction(ModuleHandle, "_AITalkAPI_GetKana@20", AITalkAPI_GetKana);
    ok &= getFunction(ModuleHandle, "_AITalkAPI_GetParam@8", AITalkAPI_GetParam);
    ok &= getFunction(ModuleHandle, "_AITalkAPI_Init@4", AITalkAPI_Init);
    ok &= getFunction(ModuleHandle, "_AITalkAPI_LangClear@0", AITalkAPI_LangClear);
    ok &= getFunction(ModuleHandle, "_AITalkAPI_LangLoad@4", AITalkAPI_LangLoad);
    ok &= getFunction(ModuleHandle, "_AITalkAPI_LicenseDate@4", AITalkAPI_LicenseDate);
    ok &= getFunction(ModuleHandle, "_AITalkAPI_LicenseInfo@16", AITalkAPI_LicenseInfo);
    ok &= getFunction(ModuleHandle, "_AITalkAPI_ReloadPhraseDic@4", AITalkAPI_ReloadPhraseDic);
    ok &= getFunction(ModuleHandle, "_AITalkAPI_ReloadSymbolDic@4", AITalkAPI_ReloadSymbolDic);
    ok &= getFunction(ModuleHandle, "_AITalkAPI_ReloadWordDic@4", AITalkAPI_ReloadWordDic);
    ok &= getFunction(ModuleHandle, "_AITalkAPI_SetParam@4", AITalkAPI_SetParam);
    ok &= getFunction(ModuleHandle, "_AITalkAPI_TextToKana@12", AITalkAPI_TextToKana);
    ok &= getFunction(ModuleHandle, "_AITalkAPI_TextToSpeech@12", AITalkAPI_TextToSpeech);
    ok &= getFunction(ModuleHandle, "_AITalkAPI_VersionInfo@16", AITalkAPI_VersionInfo);
    ok &= getFunction(ModuleHandle, "_AITalkAPI_VoiceClear@0", AITalkAPI_VoiceClear);
    ok &= getFunction(ModuleHandle, "_AITalkAPI_VoiceLoad@4", AITalkAPI_VoiceLoad);
    if (ok == false) {
        InitializeAll();
        SetLastError(String::Format(L"モジュール'{0}'から関数の取得に失敗しました。", module_path_managed));
        return false;
    }
    
    // ライブラリを初期化する
    std::string voice_db_dir = context.marshal_as<const char*>(InstallDirectory + L"\\Voice");
    std::string license_path = context.marshal_as<const char*>(InstallDirectory + L"\\aitalk.lic");
    AITalk_TConfig config;
    config.hzVoiceDB = VoiceDbSampleRate;
    config.dirVoiceDBS = voice_db_dir.c_str();
    config.msecTimeout = timeout;
    config.pathLicense = license_path.c_str();
    config.codeAuthSeed = context.marshal_as<const char*>(auth_code_seed);
    config.__reserved__ = 0;
    AITalkResultCode result;
    result = AITalkAPI_Init(&config);
    if (result != AITALKERR_SUCCESS) {
        InitializeAll();
        SetLastError(String::Format(L"モジュール'{0}'の初期化に失敗しました。(エラーコード:{1})", module_path_managed, static_cast<int>(result)));
        return false;
    }

    ClearLastError();
    return true;
}

void AITalkWrapper::AITalkWrapper::CloseLibrary(void) {
    if (IsLibraryOpened() == true) {
        AITalkAPI_End();
    }
    InitializeAll();
}

bool AITalkWrapper::AITalkWrapper::IsLibraryOpened(void) {
    return (ModuleHandle != NULL);
}

bool AITalkWrapper::AITalkWrapper::LoadLanguage(String ^language_name) {
    if (IsLibraryOpened() == false) {
        SetLastError(gcnew String(ERRORMESSAGE_MODULE_NOT_LOADED));
        return false;
    }

    // 現在のカレントディレクトリを記憶する
    String ^backup = System::IO::Directory::GetCurrentDirectory();
    
    // カレントディレクトリを一時的にVOICEROID2のインストールディレクトリに変更する
    // それ以外ではAITalkAPI_LangLoad()はエラーを返す
    try {
        System::IO::Directory::SetCurrentDirectory(InstallDirectory);
    } catch (Exception ^e) {
        SetLastError(String::Format(L"カレントディレクトリを'{0}'へ変更することに失敗しました。({1})", InstallDirectory, e->Message));
        return false;
    }
    
    // 言語ファイルを読み込ませる
    marshal_context context;
    std::string language_path = context.marshal_as<const char*>(InstallDirectory + L"\\Lang\\" + language_name);
    AITalkResultCode result;
    result = AITalkAPI_LangLoad(language_path.c_str());

    // カレントディレクトリを戻す
    try {
        System::IO::Directory::SetCurrentDirectory(backup);
    } catch (Exception ^e) {
        SetLastError(String::Format(L"カレントディレクトリを'{0}'へ戻すことに失敗しました。({1})", backup, e->Message));
        return false;
    }

    if (result != AITALKERR_SUCCESS) {
        SetLastError(String::Format(L"言語ファイル'{0}'の読み込みに失敗しました。(エラーコード:{1})", language_name, static_cast<int>(result)));
        return false;
    }

    ClearLastError();
    return true;
}

bool AITalkWrapper::AITalkWrapper::LoadPhraseDictionary(String ^path) {
    if (IsLibraryOpened() == false) {
        SetLastError(gcnew String(ERRORMESSAGE_MODULE_NOT_LOADED));
        return false;
    }
    AITalkResultCode result;
    marshal_context context;
    AITalkAPI_ReloadPhraseDic(nullptr);
    result = AITalkAPI_ReloadPhraseDic(context.marshal_as<const char*>(path));
    if (result == AITALKERR_SUCCESS) {
        return true;
    } else if (result == AITALKERR_USERDIC_NOENTRY) {
        AITalkAPI_ReloadPhraseDic(nullptr);
        return true;
    } else {
        SetLastError(String::Format(L"フレーズ辞書'{0}'の読み込みに失敗しました。(エラーコード:{1})", path, static_cast<int>(result)));
        return false;
    }
    ClearLastError();
    return true;
}

bool AITalkWrapper::AITalkWrapper::LoadWordDictionary(String ^path) {
    if (IsLibraryOpened() == false) {
        SetLastError(gcnew String(ERRORMESSAGE_MODULE_NOT_LOADED));
        return false;
    }
    AITalkResultCode result;
    marshal_context context;
    AITalkAPI_ReloadWordDic(nullptr);
    result = AITalkAPI_ReloadWordDic(context.marshal_as<const char*>(path));
    if (result == AITALKERR_SUCCESS) {
        return true;
    } else if (result == AITALKERR_USERDIC_NOENTRY) {
        AITalkAPI_ReloadWordDic(nullptr);
        return true;
    } else {
        SetLastError(String::Format(L"単語辞書'{0}'の読み込みに失敗しました。(エラーコード:{1})", path, static_cast<int>(result)));
        return false;
    }
    ClearLastError();
    return true;
}

bool AITalkWrapper::AITalkWrapper::LoadSymbolDictionary(String ^path) {
    if (IsLibraryOpened() == false) {
        SetLastError(gcnew String(ERRORMESSAGE_MODULE_NOT_LOADED));
        return false;
    }
    AITalkResultCode result;
    marshal_context context;
    AITalkAPI_ReloadSymbolDic(nullptr);
    result = AITalkAPI_ReloadSymbolDic(context.marshal_as<const char*>(path));
    if (result == AITALKERR_SUCCESS) {
        return true;
    } else if (result == AITALKERR_USERDIC_NOENTRY) {
        AITalkAPI_ReloadSymbolDic(nullptr);
        return true;
    } else {
        SetLastError(String::Format(L"記号ポーズ辞書'{0}'の読み込みに失敗しました。(エラーコード:{1})", path, static_cast<int>(result)));
        return false;
    }
    ClearLastError();
    return true;
}

bool AITalkWrapper::AITalkWrapper::LoadVoice(String ^voice_name) {
    if (IsLibraryOpened() == false) {
        SetLastError(gcnew String(ERRORMESSAGE_MODULE_NOT_LOADED));
        return false;
    }

    // ボイスライブラリを読み込む
    AITalkResultCode result;
    marshal_context context;
    result = AITalkAPI_VoiceLoad(context.marshal_as<const char*>(voice_name));
    if (result != AITALKERR_SUCCESS) {
        SetLastError(String::Format(L"ボイスライブラリ'{0}'の読み込みに失敗しました。(エラーコード:{1})", voice_name, static_cast<int>(result)));
        return false;
    }

    // 動作パラメータの初期値を取得する
    uint32_t size = 0;
    result = AITalkAPI_GetParam(nullptr, &size);
    if ((result != AITALKERR_INSUFFICIENT) || (static_cast<size_t>(size) < sizeof(AITalk_TTtsParam))) {
        AITalkAPI_VoiceClear();
        SetLastError(String::Format(L"動作パラメータの長さの取得に失敗しました。(エラーコード:{0})", static_cast<int>(result)));
        return false;
    }
    std::vector<uint8_t> param_buffer(size);
    AITalk_TTtsParam *param = reinterpret_cast<AITalk_TTtsParam*>(param_buffer.data());
    param->size = size;
    result = AITalkAPI_GetParam(param, &size);
    if (result != AITALKERR_SUCCESS) {
        AITalkAPI_VoiceClear();
        SetLastError(String::Format(L"動作パラメータの取得に失敗しました。(エラーコード:{0})", static_cast<int>(result)));
        return false;
    }

    // コールバック関数を設定する
    param->procTextBuf = CallbackTextBuf;
    param->procRawBuf = CallbackRawBuf;
    param->procEventTts = CallbackEventTts;
    param->extendFormat = static_cast<ExtendFormat>(JeitaRuby | AutoBookmark);
    result = AITalkAPI_SetParam(param);
    if (result != AITALKERR_SUCCESS) {
        AITalkAPI_VoiceClear();
        SetLastError(String::Format(L"動作パラメータの設定に失敗しました。(エラーコード:{0})", static_cast<int>(result)));
        return false;
    }

    ClearLastError();
    return true;
}

String^ AITalkWrapper::AITalkWrapper::TextToKana(String ^text, int timeout) {
    std::unique_ptr< KanaConverter_t> converter(new KanaConverter_t);
    converter->GetKana = AITalkAPI_GetKana;
    
    // UnicodeとShift-JISの対応を記憶する
    std::string ascii_string;
    std::vector<int> ascii_to_unicode;
    {
        array<wchar_t>^ text_char_array = text->ToCharArray();
        pin_ptr<wchar_t> text_ptr = &text_char_array[0];
        std::wstring unicode_string(text_ptr, text_char_array->Length);
        if (UnicodeToShiftJIS(unicode_string, &ascii_string, &ascii_to_unicode) == false) {
            return nullptr;
        }
    }

    // 変換を開始する
    AITalk_TJobParam job_param;
    job_param.modeInOut = AITALKIOMODE_PLAIN_TO_AIKANA;
    job_param.userData = converter.get();
    int32_t job_id = 0;
    AITalkResultCode result;
    result = AITalkAPI_TextToKana(&job_id, &job_param, ascii_string.c_str());
    if (result != AITALKERR_SUCCESS) {
        SetLastError(String::Format(L"仮名変換が開始できませんでした。(エラーコード:{0})", static_cast<int>(result)));
        return nullptr;
    }

    // 変換の終了を待つ
    // timeoutで与えられた時間だけ待つ
    DWORD timeout_winapi = (0 < timeout) ? timeout : INFINITE;
    DWORD result_winapi;
    result_winapi = WaitForSingleObject(converter->CloseEventHandle, timeout_winapi);

    // 変換を終了する
    result = AITalkAPI_CloseKana(job_id, 0);
    if (result_winapi != WAIT_OBJECT_0) {
        SetLastError(L"仮名変換がタイムアウトしました。");
        return nullptr;
    }else if (result != AITALKERR_SUCCESS) {
        SetLastError(String::Format(L"仮名変換が正常に終了しませんでした。(エラーコード:{0})", static_cast<int>(result)));
        return nullptr;
    }

    // 結果に含まれるIrq MARKのバイト位置をUnicodeの文字の位置へ置き換える
    std::string &output = converter->Output;
    std::string output_replaced;
    size_t offset = 0;
    while (true) {
        static const char StartOfIrqMark[] = "(Irq MARK=_AI@";
        size_t pos = output.find(StartOfIrqMark, offset);
        if (pos == std::string::npos) {
            output_replaced.append(&output[offset], &output[output.size()]);
            break;
        }
        pos += strlen(StartOfIrqMark);
        output_replaced.append(&output[offset], &output[pos]);
        char *end_ptr;
        long byte_index = strtol(&output[pos], &end_ptr, 10);
        if ((byte_index < 0) || (ascii_to_unicode.size() <= static_cast<size_t>(byte_index))) {
            SetLastError(L"文節位置の特定に失敗しました。");
            return nullptr;
        }
        output_replaced.append(std::to_string(ascii_to_unicode[byte_index]));
        offset = end_ptr - &output[0];
    }

    ClearLastError();
    return gcnew String(output_replaced.c_str());
}

array<Byte>^ AITalkWrapper::AITalkWrapper::KanaToSpeech(String ^kana, int timeout) {
    array<Tuple<UInt64, String^>^> ^event;
    return KanaToSpeech(kana, timeout, event);
}

array<Byte>^ AITalkWrapper::AITalkWrapper::KanaToSpeech(String ^kana, int timeout, [Out] array<Tuple<UInt64, String^>^> ^%event) {
    std::unique_ptr< SpeechConverter_t> converter(new SpeechConverter_t);
    converter->GetData = AITalkAPI_GetData;
    
    // 変換を開始する
    AITalk_TJobParam job_param;
    job_param.modeInOut = AITALKIOMODE_AIKANA_TO_WAVE;
    job_param.userData = converter.get();
    int32_t job_id = 0;
    AITalkResultCode result;
    marshal_context context;
    result = AITalkAPI_TextToSpeech(&job_id, &job_param, context.marshal_as<const char*>(kana));
    if (result != AITALKERR_SUCCESS) {
        SetLastError(String::Format(L"音声変換が開始できませんでした。(エラーコード:{0})", static_cast<int>(result)));
        return nullptr;
    }

    // 変換の終了を待つ
    // timeoutで与えられた時間だけ待つ
    DWORD timeout_winapi = (0 < timeout) ? timeout : INFINITE;
    DWORD result_winapi;
    result_winapi = WaitForSingleObject(converter->CloseEventHandle, timeout_winapi);

    // 変換を終了する
    result = AITalkAPI_CloseSpeech(job_id, 0);
    if (result_winapi != WAIT_OBJECT_0) {
        SetLastError(L"音声変換がタイムアウトしました。");
        return nullptr;
    } else if (result != AITALKERR_SUCCESS) {
        SetLastError(String::Format(L"音声変換が正常に終了しませんでした。(エラーコード:{0})", static_cast<int>(result)));
        return nullptr;
    }

    // 出力にコピーする
    array<Byte>^ output = gcnew array<Byte>(44 + converter->Output.size());
    {
        pin_ptr<Byte> output_ptr = &output[0];
        memcpy(output_ptr + 0, "RIFF", 4);
        *reinterpret_cast<uint32_t*>(output_ptr + 4) = 36 + converter->Output.size();
        memcpy(output_ptr + 8, "WAVEfmt ", 8);
        *reinterpret_cast<uint32_t*>(output_ptr + 16) = 16;
        *reinterpret_cast<uint16_t*>(output_ptr + 20) = 0x1;
        *reinterpret_cast<uint16_t*>(output_ptr + 22) = 1;
        *reinterpret_cast<uint32_t*>(output_ptr + 24) = VoiceDbSampleRate;
        *reinterpret_cast<uint32_t*>(output_ptr + 28) = 2 * VoiceDbSampleRate;
        *reinterpret_cast<uint16_t*>(output_ptr + 32) = 2;
        *reinterpret_cast<uint16_t*>(output_ptr + 34) = 16;
        memcpy(output_ptr + 36, "data", 4);
        *reinterpret_cast<uint32_t*>(output_ptr + 40) = converter->Output.size();
        memcpy(output_ptr + 44, converter->Output.data(), converter->Output.size());
    }

    // イベントをコピーする
    if (event != nullptr) {
        event = gcnew array<Tuple<UInt64, String^>^>(converter->EventOutput.size());
        if (converter->EventOutput.empty() == false) {
            for (size_t index = 0; index < converter->EventOutput.size(); index++) {
               uint64_t tick = converter->EventOutput[index].first;
               std::string &name = converter->EventOutput[index].second;
               event[index] = gcnew Tuple<UInt64, String^>(tick, gcnew String(name.c_str()));
            }
        }
    }

    ClearLastError();
    return output;
}

static int __stdcall CallbackTextBuf(AITalkEventReasonCode reason_code, int32_t job_id, void *user_data) {
    KanaConverter_t *conveter = reinterpret_cast<KanaConverter_t*>(user_data);
    if (conveter == nullptr) {
        throw;
    }
    if ((reason_code == AITALKEVENT_TEXTBUF_FULL) || (reason_code == AITALKEVENT_TEXTBUF_FLUSH) || (reason_code == AITALKEVENT_TEXTBUF_CLOSE)) {
        char *buffer = conveter->Buffer.data();
        const uint32_t buffer_size = static_cast<uint32_t>(conveter->Buffer.size());
        uint32_t read_bytes;
        do {
            AITalkResultCode result;
            uint32_t pos;
            result = conveter->GetKana(job_id, buffer, buffer_size, &read_bytes, &pos);
            if (result != AITALKERR_SUCCESS) {
                break;
            }
            conveter->Output.append(conveter->Buffer.data(), read_bytes);
        } while ((buffer_size - 1) <= read_bytes);
        if (reason_code != AITALKEVENT_TEXTBUF_CLOSE) {
            return 0;
        }
    }
    SetEvent(conveter->CloseEventHandle);
    return 0;
}

static int __stdcall CallbackRawBuf(AITalkEventReasonCode reason_code, int32_t job_id, uint64_t tick, void *user_data) {
    SpeechConverter_t *conveter = reinterpret_cast<SpeechConverter_t*>(user_data);
    if (conveter == nullptr) {
        throw;
    }
    if ((reason_code == AITALKEVENT_RAWBUF_FULL) || (reason_code == AITALKEVENT_RAWBUF_FLUSH) || (reason_code == AITALKEVENT_RAWBUF_CLOSE)) {
        int16_t *buffer = reinterpret_cast<int16_t*>(conveter->Buffer.data());
        uint32_t buffer_size = static_cast<uint32_t>(conveter->Buffer.size()) / 2;
        uint32_t read_samples;
        do {
            AITalkResultCode result;
            result = conveter->GetData(job_id, buffer, buffer_size, &read_samples);
            if (result != AITALKERR_SUCCESS) {
                break;
            }
            conveter->Output.insert(conveter->Output.end(), conveter->Buffer.begin(), conveter->Buffer.begin() + 2 * read_samples);
        } while (buffer_size <= read_samples);
        if (reason_code != AITALKEVENT_RAWBUF_CLOSE) {
            return 0;
        }
    }
    SetEvent(conveter->CloseEventHandle);
    return 0;
}

static int __stdcall CallbackEventTts(AITalkEventReasonCode reason_code, int32_t job_id, uint64_t tick, const char *name, void *user_data) {
    //printf("ProcEventTTS(%d, %d, %lld, '%s', %p)\n", reason_code, job_id, tick, name, user_data);
    SpeechConverter_t *conveter = reinterpret_cast<SpeechConverter_t*>(user_data);
    if (conveter == nullptr) {
        throw;
    }
    if (reason_code == AITALKEVENT_PH_LABEL) {
        // 発音ラベルが返される
        // 発音の開始時間とそのときの発音記号をペアにして記憶する
        conveter->EventOutput.push_back(std::pair<uint64_t, std::string>(tick, name));
    } else if (reason_code == AITALKEVENT_BOOKMARK) {
        printf("ProcEventTTS(%d, %d, %lld, '%s', %p)\n", reason_code, job_id, tick, name, user_data);
    } else if (reason_code == AITALKEVENT_AUTOBOOKMARK) {
        // 入力された文章において、その文節の終わる位置が返される(Shift-JISにおいて)
        // 文節の開始時間と、"@"+その文節の終わる位置、を記憶する
        char buf[16];
        sprintf_s(buf, "@%s", name);
        conveter->EventOutput.push_back(std::pair<uint64_t, std::string>(tick, buf));
    }
    return 0;
}

bool AITalkWrapper::AITalkWrapper::UnicodeToShiftJIS(const std::wstring &unicode_string, std::string *ascii_string, std::vector<int> *ascii_to_unicode) {
    // UnicodeをShift-JISに変換する
    std::vector<char> ascii_string_buffer(2 * unicode_string.size() + 2); // 1バイト多めに確保する
    BOOL difficult_to_read;
    int written_bytes;
    written_bytes = WideCharToMultiByte(
        CP_ACP, 
        WC_NO_BEST_FIT_CHARS, 
        unicode_string.c_str(), 
        unicode_string.length(),
        ascii_string_buffer.data(),
        ascii_string_buffer.size(), 
        NULL, 
        &difficult_to_read);
    if (written_bytes <= 0) {
        SetLastError(L"文字コードの変換の途中にエラーが発生しました。");
        return false;
    }
    if (static_cast<int>(ascii_string_buffer.size()) <= written_bytes) {
        SetLastError(L"文字コードの変換バッファが不足しました。");
        return false;
    }
    if (difficult_to_read == TRUE) {
        SetLastError(L"Shift-JISで表すことのできない文字は話せません。");
        return false;
    }
    ascii_string->assign(ascii_string_buffer.data(), ascii_string_buffer.size());

    // Shift-JISの各バイトが何文字目が計算する
    int char_index = 0;
    int byte_index = 0;
    ascii_to_unicode->resize(ascii_string->size() + 1);
    while(true){
        uint8_t c = (*ascii_string)[byte_index];
        (*ascii_to_unicode)[byte_index] = char_index;
        if (c == '\0') {
            break;
        }
        if (((0x81 <= c) && (c <= 0x9F)) || ((0xE0 <= c) && (c <= 0xEF))) {
            // Shift-JISの1バイト目なので次のバイトを読み込む
            byte_index++;
            (*ascii_to_unicode)[byte_index] = char_index;
            c = (*ascii_string)[byte_index];
            if (((c < 0x40) || (0x7E < c)) && ((c < 0x80) || (0xFC < c))) {
                SetLastError(L"このプログラムは誤ったShift-JISの取り扱いをしています。");
                return false;
            }
        }
        byte_index++;
        char_index++;
    }
    if (unicode_string.size() != static_cast<size_t>((*ascii_to_unicode)[byte_index])) {
        SetLastError(L"UnicodeとShift-JISの対応関係の計算に失敗しました。");
        return false;
    }

    return true;
}
