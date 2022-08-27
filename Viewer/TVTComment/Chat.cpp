#include "stdafx.h"
#include "Chat.h"
namespace TVTComment {
	Chat::Chat(const std::string &text, const Color &color, Position position, Size size):
		text(text),position(position),size(size),color(color)
	{
	}
}