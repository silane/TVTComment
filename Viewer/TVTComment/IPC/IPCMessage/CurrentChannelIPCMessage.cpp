#include "stdafx.h"
#include "CurrentChannelIPCMessage.h"

namespace TVTComment
{
	std::string CurrentChannelIPCMessage::GetMessageName() const
	{
		return u8"CurrentChannel";
	}

	std::vector<std::string> CurrentChannelIPCMessage::Encode() const
	{
		std::vector<std::string> ret;

		ret.push_back(std::to_string(this->SpaceIndex));
		ret.push_back(std::to_string(this->ChannelIndex));
		ret.push_back(std::to_string(this->RemotecontrolkeyId));

		ret.push_back(std::to_string(this->NetworkId));
		ret.push_back(std::to_string(this->TransportstreamId));
		ret.push_back(std::to_string(this->ServiceId));
		ret.push_back(std::to_string(this->EventId));

		ret.push_back(this->NetworkName);
		ret.push_back(this->TransportstreamName);
		ret.push_back(this->ServiceName);
		ret.push_back(this->ChannelName);

		ret.push_back(this->EventName);
		ret.push_back(this->EventText);
		ret.push_back(this->EventExtText);
		ret.push_back(std::to_string(this->StartTime));
		ret.push_back(std::to_string(this->Duration));

		return ret;
	}

	void CurrentChannelIPCMessage::Decode(const std::vector<std::string> &)
	{
		throw std::logic_error("CurrentChannelIPCMessage::Decode is not implemented");
	}
}