#pragma once

namespace TVTComment
{
	class Utils
	{
	public:
		Utils() = delete;

		static bool CloseProcessById(DWORD processId);
	};
}