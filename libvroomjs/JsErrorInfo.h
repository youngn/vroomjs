#pragma once

#include "vroomjs.h"
#include "JsValue.h"

struct jsstackframe
{
    jsstackframe(int32_t line, int32_t column, char16_t* resource, char16_t* function)
        :line(line), column(column), resource(resource), function(function), next(nullptr) {
    }

    ~jsstackframe() {
        if (resource)
            delete[] resource;
        if (function)
            delete[] function;
        if (next)
            delete next;
    }

    int32_t line;
    int32_t column;
    char16_t* resource;
    char16_t* function;

    jsstackframe* next; // linked list
};

struct jserrorinfo
{
    jsvalue error;
    int32_t line;
    int32_t column;
    char16_t* resource;
    char16_t* description;
    char16_t* type;
    char16_t* text;
    char16_t* stackstr;
    jsstackframe* stackframes; // head of linked list
};



class JsErrorInfo {
    // The position of the fields are important, as instances of this type
    // are marshaled to .NET.
    // Do NOT add any virtual members, as the inclusion of a v-table will
    // offset the fields.

public:
    static JsErrorInfo* JsErrorInfo::Capture(TryCatch& trycatch, JsContext* context);

    JsErrorInfo(char16_t* description, int32_t line, int32_t column,
        char16_t* resource, char16_t* type, char16_t* text, jsvalue error, char16_t* stackstr, jsstackframe* stackframes) {
        v.description = description;
        v.line = line;
        v.column = column;
        v.resource = resource;
        v.type = type;
        v.text = text;
        v.error = error;
        v.stackstr = stackstr;
        v.stackframes = stackframes;
    }

    JsErrorInfo(const jserrorinfo& value) {
        v = value;
    }

    operator jserrorinfo() const {
        return v;
    }

    ~JsErrorInfo() {
        if (v.description)
            delete[] v.description;
        if (v.resource)
            delete[] v.resource;
        if (v.type)
            delete[] v.type;
        if (v.text)
            delete[] v.text;
        if (v.stackframes)
            delete v.stackframes;

        ((JsValue*)&v.error)->Dispose();
    }

private:
    static jsstackframe* CaptureStackFrames(Local<StackTrace> stackTrace, JsContext* context);
    static char16_t* CreateString(Local<String> value, JsContext* context);

    jserrorinfo v;
};
