#include "stdafx.h"
#include "win32filestream.h"

template<typename charT,class traits>
basic_win32filestream<charT,traits>::basic_win32filestream(HANDLE handle,bool close_handle)
	:std::basic_iostream<charT,traits>(new basic_win32filebuf<charT,traits>(handle,close_handle))
{
}

template<typename charT, class traits>
HANDLE basic_win32filestream<charT,traits>::gethandle() const noexcept
{
	return ((basic_win32filebuf<charT, traits> *)this->rdbuf())->gethandle();
}

template<typename charT, class traits>
basic_win32filestream<charT, traits>::~basic_win32filestream() noexcept
{
	delete this->rdbuf();
}

template basic_win32filestream<char>;
template basic_win32filestream<wchar_t>;