#pragma once
#include <string>
#include <exception>

class IPCMessageDecodeError :public std::exception
{
private:
	std::string what_arg;
public:
	explicit IPCMessageDecodeError(const std::string &what_arg);

	virtual const char *what() const override;
};