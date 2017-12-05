#include "stdafx.h"
#include "IPCTunnel.h"
#include "IPCMessageFactory.h"
#include <system_error>
#include <memory>

namespace TVTComment
{
	IPCTunnel::IPCTunnel(const std::wstring &sendPipeName, const std::wstring &receivePipeName)
		:upstream(new win32filestream(CreateNamedPipe(receivePipeName.c_str(), PIPE_ACCESS_DUPLEX,PIPE_TYPE_BYTE,1,2048,2048,1000,NULL)),false)
		,downstream(new win32filestream(CreateNamedPipe(sendPipeName.c_str(), PIPE_ACCESS_DUPLEX, PIPE_TYPE_BYTE, 1, 2048, 2048, 1000, NULL)),false)
		,connectThread(INVALID_HANDLE_VALUE),sendThread(INVALID_HANDLE_VALUE),receiveThread(INVALID_HANDLE_VALUE)
	{
		this->upstream.GetBaseStream()->exceptions(win32filestream::failbit | win32filestream::badbit);
		this->downstream.GetBaseStream()->exceptions(win32filestream::failbit | win32filestream::badbit);
	}

	void IPCTunnel::Connect()
	{
		auto finally = [this](HANDLE) {this->connectThread = INVALID_HANDLE_VALUE; };
		std::unique_ptr<void, decltype(finally)> block(0, finally);
		this->connectThread = ::GetCurrentThread();

		BOOL ret = ::ConnectNamedPipe(((win32filestream *)this->upstream.GetBaseStream())->gethandle(), NULL);
		if(ret==0)
		{
			auto errorNum = ::GetLastError();
			throw std::system_error(errorNum, std::system_category());
		}

		ret = ::ConnectNamedPipe(((win32filestream *)this->downstream.GetBaseStream())->gethandle(), NULL);
		if (ret == 0)
		{
			auto errorNum = ::GetLastError();
			throw std::system_error(errorNum, std::system_category());
		}
	}

	void IPCTunnel::Send(const IIPCMessage &msg)
	{
		auto finally = [this](HANDLE) {this->sendThread = INVALID_HANDLE_VALUE; };
		std::unique_ptr<void, decltype(finally)> block(0, finally);
		this->sendThread = ::GetCurrentThread();

		RawIPCMessage rawmsg;
		rawmsg.MessageName = msg.GetMessageName();
		rawmsg.Contents = msg.Encode();
		this->downstream.Write(rawmsg);
	}

	std::unique_ptr<IIPCMessage> IPCTunnel::Receive()
	{
		auto finally = [this](HANDLE) {this->receiveThread = INVALID_HANDLE_VALUE; };
		std::unique_ptr<void, decltype(finally)> block(0, finally);
		this->receiveThread = ::GetCurrentThread();

		RawIPCMessage rawmsg;
		rawmsg = this->upstream.Read();
		return MakeIPCMessageFromRaw(rawmsg);
	}

	void IPCTunnel::Cancel() noexcept
	{
		if (this->connectThread != INVALID_HANDLE_VALUE)
			::CancelSynchronousIo(this->connectThread);
		if (this->sendThread != INVALID_HANDLE_VALUE)
			::CancelSynchronousIo(this->sendThread);
		if (this->receiveThread != INVALID_HANDLE_VALUE)
			::CancelSynchronousIo(this->receiveThread);
	}

	IPCTunnel::~IPCTunnel() noexcept
	{
		this->Cancel();
		delete this->upstream.GetBaseStream();
		delete this->downstream.GetBaseStream();
	}
}