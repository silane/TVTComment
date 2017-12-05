#include "stdafx.h"
#include "RawIPCMessage.h"

namespace TVTComment
{
	std::string RawIPCMessage::ToString() const
	{
		std::string ret;
		for (const std::string &content : this->Contents)
			ret += content+" ";
		ret.pop_back();
		return this->MessageName + ret;
	}
}