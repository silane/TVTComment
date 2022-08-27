#include "stdafx.h"
#include "IPCMessageDecodeError.h"


IPCMessageDecodeError::IPCMessageDecodeError(const std::string &what_arg) :what_arg(what_arg)
{
}

const char *IPCMessageDecodeError::what() const
{
	return what_arg.c_str();
}