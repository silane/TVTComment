#include "stdafx.h"
#include "TimeIPCMessage.h"

namespace TVTComment
{
	std::string TimeIPCMessage::GetMessageName() const
	{
		return u8"Time";
	}

	std::vector<std::string> TimeIPCMessage::Encode() const
	{
		return { std::to_string(this->Time) };
	}

	void TimeIPCMessage::Decode(const std::vector<std::string> &)
	{
		throw std::logic_error("TimeIPCMessage::Decode is not implemented");
	}
}