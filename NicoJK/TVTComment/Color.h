#pragma once
#include "../stdafx.h"

namespace TVTComment
{
	class Color
	{
	public:
		unsigned char R;
		unsigned char G;
		unsigned char B;

	public:
		static Color FromColorRef(COLORREF colorRef);

		Color() noexcept;
		Color(unsigned char r, unsigned char g, unsigned char b) noexcept;
		COLORREF GetColorRef() const noexcept;
	};
}