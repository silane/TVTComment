#include "stdafx.h"
#include "IPCProtocolStream.h"
#include <string>

namespace TVTComment
{
	std::iostream *IPCProtocolStream::GetBaseStream() const noexcept
	{
		return this->baseStream;
	}

	IPCProtocolStream::IPCProtocolStream(std::iostream *baseStream, bool deleteStream)
		:baseStream(baseStream),deleteStream(deleteStream)
	{
	}

	RawIPCMessage IPCProtocolStream::Read()
	{
		RawIPCMessage ret;
		std::string line;
		std::getline(*(this->baseStream),line,u8'\u001E');
		line += u8'\u001F';
		std::string::size_type hoge = line.find(u8'\u001F');
		ret.MessageName = line.substr(0, hoge);
		for (std::string::size_type i = hoge+1, j = line.find(u8'\u001F',i); j != std::string::npos; i = j + 1, j = line.find(u8'\u001F', i))
		{
			ret.Contents.push_back(line.substr(i, j-i));
		}
		return ret;
	}

	void IPCProtocolStream::Write(const RawIPCMessage &msg)
	{
		*(this->baseStream) << msg.MessageName;
		for (const std::string &content : msg.Contents)
			*(this->baseStream) << '\u001F' + content;
		*(this->baseStream) << '\u001E';
		this->baseStream->flush();
	}

	IPCProtocolStream::~IPCProtocolStream() noexcept
	{
		try
		{
			if (deleteStream)
				delete baseStream;
		}catch(...){}
	}
}