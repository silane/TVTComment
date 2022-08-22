#pragma once

#include "win32filebuf.h"
#include <ostream>
#include <istream>
#include <memory>

template<typename charT,typename traits=std::char_traits<charT>>
class basic_win32filestream:public std::basic_iostream<charT,traits>
{
public:
	basic_win32filestream(HANDLE handle, bool close_handle=true);
	HANDLE gethandle() const noexcept;
	~basic_win32filestream() noexcept;
};

typedef basic_win32filestream<char> win32filestream;
typedef basic_win32filestream<wchar_t> wwin32filestream;