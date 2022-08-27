#include "stdafx.h"
#include "Color.h"

namespace TVTComment
{
	Color Color::FromColorRef(COLORREF colorRef)
	{
		return Color(GetRValue(colorRef), GetGValue(colorRef), GetBValue(colorRef));
	}

	Color::Color() noexcept:R(0),G(0),B(0)
	{}

	Color::Color(unsigned char r,unsigned char g,unsigned char b) noexcept
		:R(r),G(g),B(b)
	{}

	COLORREF Color::GetColorRef() const noexcept
	{
		return RGB(R, G, B);
	}
}