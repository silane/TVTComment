#pragma once
#include <memory>
#include "RawIPCMessage.h"
#include "IPCMessage/IIPCMessage.h"
namespace TVTComment
{
	std::unique_ptr<IIPCMessage> MakeIPCMessageFromRaw(const RawIPCMessage &rawmsg);
}