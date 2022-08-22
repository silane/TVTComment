#pragma once
#include "IIPCMessage.h"
#include "../../Chat.h"

namespace TVTComment
{
	class ChatIPCMessage :public IIPCMessage
	{
	public:
		Chat Chat;

	public:
		virtual std::string GetMessageName() const override;
		virtual std::vector<std::string> Encode() const override;
		virtual void Decode(const std::vector<std::string> &contents) override;
	};
}