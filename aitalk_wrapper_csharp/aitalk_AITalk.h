#pragma once

#include <stdint.h>

enum AITalkEventReasonCode {
    AITALKEVENT_TEXTBUF_FULL = 101,
    AITALKEVENT_TEXTBUF_FLUSH = 102,
    AITALKEVENT_TEXTBUF_CLOSE = 103,
    AITALKEVENT_RAWBUF_FULL = 201,
    AITALKEVENT_RAWBUF_FLUSH = 202,
    AITALKEVENT_RAWBUF_CLOSE = 203,
    AITALKEVENT_PH_LABEL = 301,
    AITALKEVENT_BOOKMARK = 302,
    AITALKEVENT_AUTOBOOKMARK = 303
};

enum AITalkResultCode {
    AITALKERR_SUCCESS = 0,
    AITALKERR_INTERNAL_ERROR = -1,
    AITALKERR_UNSUPPORTED = -2,
    AITALKERR_INVALID_ARGUMENT = -3,
    AITALKERR_WAIT_TIMEOUT = -4,
    AITALKERR_NOT_INITIALIZED = -10,
    AITALKERR_ALREADY_INITIALIZED = 10,
    AITALKERR_NOT_LOADED = -11,
    AITALKERR_ALREADY_LOADED = 11,
    AITALKERR_INSUFFICIENT = -20,
    AITALKERR_PARTIALLY_REGISTERED = 21,
    AITALKERR_LICENSE_ABSENT = -100,
    AITALKERR_LICENSE_EXPIRED = -101,
    AITALKERR_LICENSE_REJECTED = -102,
    AITALKERR_TOO_MANY_JOBS = -201,
    AITALKERR_INVALID_JOBID = -202,
    AITALKERR_JOB_BUSY = -203,
    AITALKERR_NOMORE_DATA = 204,
    AITALKERR_OUT_OF_MEMORY = -206,
    AITALKERR_FILE_NOT_FOUND = -1001,
    AITALKERR_PATH_NOT_FOUND = -1002,
    AITALKERR_READ_FAULT = -1003,
    AITALKERR_COUNT_LIMIT = -1004,
    AITALKERR_USERDIC_LOCKED = -1011,
    AITALKERR_USERDIC_NOENTRY = -1012
};

enum AITalkStatusCode {
    AITALKSTAT_WRONG_STATE = -1,
    AITALKSTAT_INPROGRESS = 10,
    AITALKSTAT_STILL_RUNNING = 11,
    AITALKSTAT_DONE = 12
};

enum AITalkJobInOut {
    AITALKIOMODE_PLAIN_TO_WAVE = 11,
    AITALKIOMODE_AIKANA_TO_WAVE = 12,
    AITALKIOMODE_JEITA_TO_WAVE = 13,
    AITALKIOMODE_PLAIN_TO_AIKANA = 21,
    AITALKIOMODE_AIKANA_TO_JEITA = 32
};

enum ExtendFormat {
    None = 0,
    JeitaRuby = 1,
    AutoBookmark = 16
};

using AITalkProcTextBuf = int(__stdcall *)(AITalkEventReasonCode reasonCode, int32_t jobID, void *userData);

using AITalkProcRawBuf = int (__stdcall *)(AITalkEventReasonCode reasonCode, int32_t jobID, uint64_t tick, void *userData);

using AITalkProcEventTTS = int(__stdcall *)(AITalkEventReasonCode reasonCode, int32_t jobID, uint64_t tick, const char *name, void *userData);

#pragma pack(push, 1)
struct AITalk_TConfig {
    uint32_t hzVoiceDB;
    const char *dirVoiceDBS;
    uint32_t msecTimeout;
    const char *pathLicense;
    const char *codeAuthSeed;
    uint32_t __reserved__;
};
#pragma pack(pop)

#pragma pack(push, 1)
struct AITalk_TJobParam {
    AITalkJobInOut modeInOut;
    void *userData;
};
#pragma pack(pop)

#pragma pack(push, 1)
struct AITalk_TTtsParam {
    static const size_t MAX_VOICENAME = 80;
    static const size_t MAX_JEITACONTROL = 12;
    uint32_t size;
    AITalkProcTextBuf procTextBuf;
    AITalkProcRawBuf procRawBuf;
    AITalkProcEventTTS procEventTts;
    uint32_t lenTextBufBytes;
    uint32_t lenRawBufBytes;
    float volume;
    int32_t pauseBegin;
    int32_t pauseTerm;
    ExtendFormat extendFormat;
    char voiceName[MAX_VOICENAME];
    struct TJeitaParam {
        char femaleName[MAX_VOICENAME];
        char maleName[MAX_VOICENAME];
        int32_t pauseMiddle;
        int32_t pauseLong;
        int32_t pauseSentence;
        char control[MAX_JEITACONTROL]; // JEITA TT-6004ÇéQè∆ÇπÇÊ
    } Jeita;
    uint32_t numSpeakers;
    int32_t __reserved__;
    struct TSpeakerParam {
        char voiceName[MAX_VOICENAME];
        float volume;
        float speed;
        float pitch;
        float range;
        int pauseMiddle;
        int32_t pauseLong;
        int32_t pauseSentence;
        char styleRate[MAX_VOICENAME];
    } Speaker[1];
};
#pragma pack(pop)
