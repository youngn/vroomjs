#pragma once

#include "vroomjs.h"
#include "JsValue.h"

class JsErrorInfo {
	// The position of the fields are important, as instances of this type
	// are marshaled to .NET.
	// Do NOT add any virtual members, as the inclusion of a v-table will
	// offset the fields.

public:
	static JsErrorInfo* JsErrorInfo::Capture(TryCatch& trycatch, JsContext* context);

	JsErrorInfo(uint16_t* message, int32_t line, int32_t column, uint16_t* resource, uint16_t* type, jsvalue error) {
		v.message = message;
		v.line = line;
		v.column = column;
		v.resource = resource;
		v.type = type;
		v.error = error;
	}

	JsErrorInfo(const jserrorinfo& value) {
		v = value;
	}

	operator jserrorinfo() const {
		return v;
	}

	~JsErrorInfo() {
		if (v.message)
			delete[] v.message;
		if (v.resource)
			delete[] v.resource;
		if (v.type)
			delete[] v.type;
		((JsValue*)&v.error)->Dispose();
	}

private:
	static uint16_t* CreateString(Local<String> value, JsContext* context);

	jserrorinfo v;
};
