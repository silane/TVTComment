#include "stdafx.h"
#include "CloseIPCMessage.h"

namespace TVTComment
{
	std::string CloseIPCMessage::GetMessageName() const
	{
		return u8"Close";
	}
	std::vector<std::string> CloseIPCMessage::Encode() const
	{
		return{};
	}
	void CloseIPCMessage::Decode(const std::vector<std::string> &)
	{
	}
}