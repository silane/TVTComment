#include "stdafx.h"
#include "ChannelListIPCMessage.h"
#include <stdexcept>

namespace TVTComment
{
	std::string ChannelListIPCMessage::GetMessageName() const
	{
		return u8"ChannelList";
	}

	std::vector<std::string> ChannelListIPCMessage::Encode() const
	{
		std::vector<std::string> ret;
		for (const ChannelInfo &channelInfo : this->ChannelList)
		{
			ret.push_back(std::to_string(channelInfo.SpaceIdx));
			ret.push_back(std::to_string(channelInfo.ChannelIdx));
			switch (channelInfo.TuningSpace)
			{
			case ChannelInfo::TuningSpaceType::Unknown:
				ret.push_back("Unknown");
				break;
			case ChannelInfo::TuningSpaceType::Terrestrial:
				ret.push_back("Terrestrial");
				break;
			case ChannelInfo::TuningSpaceType::BS:
				ret.push_back("BS");
				break;
			case ChannelInfo::TuningSpaceType::CS:
				ret.push_back("CS");
				break;
			default:
				ret.push_back("Unknown");
				break;
			}
			ret.push_back(std::to_string(channelInfo.RemoteControlKeyID));
			ret.push_back(std::to_string(channelInfo.NetworkID));
			ret.push_back(std::to_string(channelInfo.TransportStreamID));
			ret.push_back(channelInfo.NetworkName);
			ret.push_back(channelInfo.TransportStreamName);
			ret.push_back(channelInfo.ChannelName);
			ret.push_back(std::to_string(channelInfo.ServiceID));
			ret.push_back(channelInfo.Hidden ? "T" : "F");
		}
		return ret;
	}

	void ChannelListIPCMessage::Decode(const std::vector<std::string> &)
	{
		throw std::logic_error("ChannelListIPCMessage::Decode is not implemented");
	}
}