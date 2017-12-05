#pragma once
#include <string>
#include <vector>

namespace TVTComment
{
	class IIPCMessage
	{
	public:

		virtual std::string GetMessageName() const = 0;

		virtual std::vector<std::string> Encode()const = 0;
		virtual void Decode(const std::vector<std::string> &contents) = 0;

		virtual ~IIPCMessage() noexcept = default;
	};
}