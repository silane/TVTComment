#pragma once
#include "IIPCMessage.h"

namespace TVTComment
{
	class CommandIPCMessage:public IIPCMessage
	{
	public:
		std::string CommandId;

		virtual std::string GetMessageName() const;
		virtual std::vector<std::string> Encode() const;
		virtual void Decode(const std::vector<std::string> &contents);
	};
}