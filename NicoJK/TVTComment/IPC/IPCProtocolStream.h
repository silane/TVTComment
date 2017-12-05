#pragma once
#include "RawIPCMessage.h"
#include <istream>
namespace TVTComment
{
	//0x1E(Record Separator),0x1F(Unit Separator)•¶š‚Í“`‘—ƒf[ƒ^‚ÉŠÜ‚ß‚Ä‚Í‚È‚ç‚È‚¢
	class IPCProtocolStream
	{
	private:
		std::iostream *baseStream;
		bool deleteStream;
	public:
		std::iostream *GetBaseStream() const noexcept;
		IPCProtocolStream(std::iostream *baseStream,bool deleteStream=true);
		RawIPCMessage Read();
		void Write(const RawIPCMessage &msg);
		~IPCProtocolStream() noexcept;
	};
}