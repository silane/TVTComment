#pragma once
#include "IIPCMessage.h"
#include "../../ChannelInfo.h"

namespace TVTComment
{
	class ChannelListIPCMessage :public IIPCMessage
	{
	public:
		std::vector<ChannelInfo> ChannelList;

	public:
		virtual std::string GetMessageName() const override;
		virtual std::vector<std::string> Encode() const override;
		virtual void Decode(const std::vector<std::string> &contents) override;
	};
}