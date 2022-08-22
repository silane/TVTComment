#pragma once
#include <streambuf>
#include <array>
#include <Windows.h>
template<typename charT,typename traits=std::char_traits<charT>>
class basic_win32filebuf:public std::basic_streambuf<charT,traits>
{
public:
	basic_win32filebuf(HANDLE handle, bool close_handle = true,bool use_overlapped=false);
	~basic_win32filebuf() noexcept;

	HANDLE gethandle() const noexcept;

public:
	virtual std::streamsize xsputn(const char_type *s, std::streamsize n) override;
	virtual std::streamsize xsgetn(char_type *s, std::streamsize n) override;

protected:
	virtual int sync() override;
	virtual int_type overflow(int_type c = traits_type::eof()) override;
	virtual int_type underflow() override;

private:
	bool dowrite();
	bool doread();

	static constexpr std::size_t BUFFER_SIZE = 4096;
	HANDLE handle;
	bool close_handle;
	bool use_overlapped;
	std::array<charT,BUFFER_SIZE> outBuffer;
	std::array<charT,BUFFER_SIZE> inBuffer;
};

typedef basic_win32filebuf<char> win32filebuf;
typedef basic_win32filebuf<wchar_t> wwin32filebuf;