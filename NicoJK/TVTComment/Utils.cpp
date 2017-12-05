#include "stdafx.h"
#include "Utils.h"

namespace
{
	struct EnumWindowsCallbackArg
	{
		DWORD ProcessId;
		bool Success;
	};

	BOOL CALLBACK EnumWindowsCallback(HWND hWnd, LPARAM lParam)
	{
		auto arg = (EnumWindowsCallbackArg *)lParam;
		arg->Success = false;

		DWORD processId = 0;
		::GetWindowThreadProcessId(hWnd, &processId);

		if (arg->ProcessId == processId)
		{
			if (::PostMessage(hWnd, WM_CLOSE, 0, 0) != 0)
				arg->Success = true;
			return FALSE;
		}
		return TRUE;
	}
}

namespace TVTComment
{
	bool Utils::CloseProcessById(DWORD processId)
	{
		EnumWindowsCallbackArg arg;
		arg.ProcessId = processId;
		arg.Success = false;
		EnumWindows(EnumWindowsCallback, (LPARAM)&arg);

		return arg.Success;
	}
}