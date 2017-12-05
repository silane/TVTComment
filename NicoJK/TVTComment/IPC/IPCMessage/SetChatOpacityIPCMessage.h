#pragma once
#include "IIPCMessage.h"

namespace TVTComment
{
	class SetChatOpacityIPCMessage :public IIPCMessage
	{
	public:
		unsigned char Opacity = 10;

	public:
		virtual std::string GetMessageName() const override;
		virtual std::vector<std::string> Encode() const override;
		virtual void Decode(const std::vector<std::string> &contents) override;
	};
}