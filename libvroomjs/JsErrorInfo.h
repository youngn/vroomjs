#pragma once

#include "vroomjs.h"
#include "JsValue.h"

class JsErrorInfo {
public:
	inline JsErrorInfo(jsvalue message, int32_t line, int32_t column, jsvalue resource, jsvalue type, jsvalue error) {
		v.message = message;
		v.line = line;
		v.column = column;
		v.resource = resource;
		v.type = type;
		v.error = error;
	}

	inline JsErrorInfo(const jserrorinfo& value) {
		v = value;
	}

	operator jserrorinfo() const {
		return v;
	}

	~JsErrorInfo() {
		((JsValue*)&v.message)->Dispose();
		((JsValue*)&v.resource)->Dispose();
		((JsValue*)&v.type)->Dispose();
		((JsValue*)&v.error)->Dispose();
	}

private:
	jserrorinfo v;
};
