#pragma once

#include "TVTComment/TVTComment.h"
#include <memory>

// プラグインクラス
class CViewer : public TVTest::CTVTestPlugin
{
public:
	// チャンネル変更などの後に適当な実況IDのチェックを行うまでの猶予
	static const int SETUP_CURJK_DELAY = 5000;
	// CTVTestPlugin
	CViewer();
	bool GetPluginInfo(TVTest::PluginInfo *pInfo);
	bool Initialize();
	bool Finalize();
private:
	struct SETTINGS {
		// memset()するためフィールドはすべてPOD型でなければならない
		int timerInterval;
		int halfSkipThreshold;
		int commentLineMargin;
		int commentFontOutline;
		int commentSize;
		int commentSizeMin;
		int commentSizeMax;
		TCHAR commentFontName[LF_FACESIZE];
		TCHAR commentFontNameMulti[LF_FACESIZE];
		bool bCommentFontBold;
		bool bCommentFontAntiAlias;
		int commentDuration;
		int commentDrawLineCount;
		bool bUseOsdCompositor;
		bool bUseTexture;
		bool bUseDrawingThread;
		int defaultPlaybackDelay;
		int forwardList[26];
		int commentOpacity;
	};
	bool TogglePlugin(bool bEnabled);
	void ToggleStreamCallback(bool bSet);
	static unsigned int __stdcall SyncThread(void *pParam);
	void LoadFromIni();
	HWND GetFullscreenWindow();
	HWND FindVideoContainer();
	bool GetCurrentTot(FILETIME *pft);
	static LRESULT CALLBACK EventCallback(UINT Event, LPARAM lParam1, LPARAM lParam2, void *pClientData);
	static BOOL CALLBACK WindowMsgCallback(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam, LRESULT *pResult, void *pUserData);
	static INT_PTR CALLBACK ForceDialogProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam);
	INT_PTR ForceDialogProcMain(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam);
	static BOOL CALLBACK StreamCallback(BYTE *pData, void *pClientData);

	// 設定ファイルの名前(Shlwapi使うのでMAX_PATHより大きくしても意味がない)
	TCHAR szIniFileName_[MAX_PATH];
	SETTINGS s_;

	// タイマーとかを管理するためのダミー窓
	HWND hDummy_;

	// コメント描画ウィンドウ
	CCommentWindow commentWindow_;
	DWORD forwardTick_;
	HANDLE hSyncThread_;
	bool bQuitSyncThread_;
	bool bPendingTimerForward_;
	bool bHalfSkip_;
	bool bFlipFlop_;
	int forwardOffset_;
	int forwardOffsetDelta_;

	// 過去ログ関係
	bool bSetStreamCallback_;
	bool bResyncComment_;
	FILETIME ftTot_[2];
	DWORD totTick_[2];
	DWORD pcr_;
	DWORD pcrTick_;
	int pcrPid_;
	int pcrPids_[8];
	int pcrPidCounts_[8];
	CCriticalLock streamLock_;

#pragma region TVTComment
	std::unique_ptr<TVTComment::TVTComment> tvtComment;
#pragma endregion
};
