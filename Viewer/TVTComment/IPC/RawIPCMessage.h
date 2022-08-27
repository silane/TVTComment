#pragma once
#include <string>
#include <vector>

namespace TVTComment
{
	class RawIPCMessage
	{
	public:
		std::string MessageName;
		std::vector<std::string> Contents;

	public:
		std::string ToString() const;
	};
}