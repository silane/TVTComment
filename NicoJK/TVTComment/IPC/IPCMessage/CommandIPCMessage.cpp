#include "stdafx.h"
#include "CommandIPCMessage.h"
#include "../IPCMessageDecodeError.h"

namespace TVTComment
{
	std::string CommandIPCMessage::GetMessageName() const
	{
		return "Command";
	}

	std::vector<std::string> CommandIPCMessage::Encode() const
	{
		return{this->CommandId};
	}

	void CommandIPCMessage::Decode(const std::vector<std::string> &contents)
	{
		if (contents.size() != 1)
			throw IPCMessageDecodeError("CommandIPCMessage‚Ìcontents‚Ì”‚ª1ˆÈŠO‚Å‚·");
		
		this->CommandId = contents[0];
	}
}