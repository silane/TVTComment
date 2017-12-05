#pragma once
#include "IIPCMessage.h"
namespace TVTComment
{
	class TimeIPCMessage :public IIPCMessage
	{
	public:
		std::time_t Time;

	public:
		virtual std::string GetMessageName() const;
		virtual std::vector<std::string> Encode() const;
		virtual void Decode(const std::vector<std::string> &contents);
	};
}