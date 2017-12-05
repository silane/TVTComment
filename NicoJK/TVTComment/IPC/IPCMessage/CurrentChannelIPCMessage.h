#pragma once
#include "IIPCMessage.h"
#include <chrono>
namespace TVTComment
{
	class CurrentChannelIPCMessage :
		public IIPCMessage
	{
	public:
		int SpaceIndex;
		int ChannelIndex;
		int RemotecontrolkeyId;

		uint16_t NetworkId;
		uint16_t TransportstreamId;
		uint16_t ServiceId;
		uint16_t EventId;
		
		std::string NetworkName;
		std::string TransportstreamName;
		std::string ServiceName;
		std::string ChannelName;

		std::string EventName;
		std::string EventText;
		std::string EventExtText;

		std::time_t StartTime;
		uint32_t Duration;

	public:
		virtual std::string GetMessageName() const;
		virtual std::vector<std::string> Encode() const override;
		virtual void Decode(const std::vector<std::string> &contents) override;
	};
}