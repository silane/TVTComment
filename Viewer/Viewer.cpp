/*
	NicoJK
		TVTest ニコニコ実況プラグイン
*/

#include "stdafx.h"
#include "NicoJK/Util.h"
#include "NicoJK/CommentWindow.h"
#define TVTEST_PLUGIN_CLASS_IMPLEMENT
#include "NicoJK/TVTestPlugin.h"
#include "NicoJK/resource.h"
#include "Viewer.h"

#ifdef _DEBUG
#include <stdarg.h>
inline void dprintf_real( const _TCHAR * fmt, ... )
{
  _TCHAR buf[1024];
  va_list ap;
  va_start(ap, fmt);
  _vsntprintf_s(buf, 1024, fmt, ap);
  va_end(ap);
  OutputDebugString(buf);
}
#  define dprintf dprintf_real
#else
#  define dprintf __noop
#endif

#define WM_RESET_STREAM (WM_APP + 105)

enum {
	TIMER_FORWARD = 1,
	TIMER_SETUP_CURJK,
	TIMER_DONE_MOVE,
	TIMER_DONE_SIZE,
};

CViewer::CViewer()
	: hDummy_(NULL)
	, forwardTick_(0)
	, hSyncThread_(NULL)
	, bQuitSyncThread_(false)
	, bPendingTimerForward_(false)
	, bHalfSkip_(false)
	, bFlipFlop_(false)
	, forwardOffset_(0)
	, forwardOffsetDelta_(0)
	, bSetStreamCallback_(false)
	, bResyncComment_(false)
	, pcr_(0)
	, pcrTick_(0)
	, pcrPid_(-1)
{
	szIniFileName_[0] = TEXT('\0');
	memset(&s_, 0, sizeof(s_));
	// TOTを取得できていないことを表す
	ftTot_[0].dwHighDateTime = 0xFFFFFFFF;
	ftTot_[1].dwHighDateTime = 0xFFFFFFFF;
	pcrPids_[0] = -1;
}

bool CViewer::GetPluginInfo(TVTest::PluginInfo *pInfo)
{
	// プラグインの情報を返す
	pInfo->Type           = TVTest::PLUGIN_TYPE_NORMAL;
	pInfo->Flags          = 0;
	pInfo->pszPluginName  = L"TvtComment";
	pInfo->pszCopyright   = L"(c) 2017 silane";
	pInfo->pszDescription = L"実況コメントをオーバーレイ表示";
	return true;
}

bool CViewer::Initialize()
{
	// 初期化処理
	if (!GetLongModuleFileName(g_hinstDLL, szIniFileName_, _countof(szIniFileName_)) ||
	    !PathRenameExtension(szIniFileName_, TEXT(".ini"))) {
		szIniFileName_[0] = TEXT('\0');
	}
	WSADATA wsaData;
	if (WSAStartup(MAKEWORD(2, 0), &wsaData) != 0) {
		return false;
	}
	// OsdCompositorは他プラグインと共用することがあるので、有効にするならFinalize()まで破棄しない
	bool bEnableOsdCompositor = GetPrivateProfileInt(TEXT("Setting"), TEXT("enableOsdCompositor"), 0, szIniFileName_) != 0;
	if (!commentWindow_.Initialize(g_hinstDLL, &bEnableOsdCompositor)) {
		WSACleanup();
		return false;
	}
	if (bEnableOsdCompositor) {
		m_pApp->AddLog(L"OsdCompositorを初期化しました。");
	}
	// コマンドを登録
	m_pApp->RegisterCommand(static_cast<int>(TVTComment::Command::ShowWindow), L"ShowWindow", L"ウィンドウの前面表示");
	// イベントコールバック関数を登録
	m_pApp->SetEventCallback(EventCallback, this);
	return true;
}

bool CViewer::Finalize()
{
	// 終了処理
	if (m_pApp->IsPluginEnabled()) {
		TogglePlugin(false);
	}
	commentWindow_.Finalize();
	WSACleanup();
	return true;
}

bool CViewer::TogglePlugin(bool bEnabled)
{
	if (bEnabled) {
		if (!hDummy_) {
			LoadFromIni();

#pragma region TVTComment
			wchar_t exePath[MAX_PATH];
			wchar_t modulePath[MAX_PATH];
			GetPrivateProfileStringW(L"TvtComment", L"ExePath", L"TvtComment/TvtComment.exe", exePath, sizeof(exePath) / sizeof(exePath[0]), szIniFileName_);
			GetLongModuleFileName(g_hinstDLL, modulePath, sizeof(modulePath) / sizeof(modulePath[0]));
			PathRemoveFileSpecW(modulePath);
			PathAppendW(modulePath, exePath);

			//tvtCommentオブジェクトは勢いウィンドウより生存期間が長い
			tvtComment = std::make_unique<TVTComment::TVTComment>(m_pApp, &commentWindow_, modulePath);
#pragma endregion

			// ダミー窓作成
			hDummy_ = CreateDialogParam(g_hinstDLL, MAKEINTRESOURCE(IDD_FORCE), NULL,
			                            ForceDialogProc, reinterpret_cast<LPARAM>(this));
			if (hDummy_) {
				// ウィンドウコールバック関数を登録
				m_pApp->SetWindowMessageCallback(WindowMsgCallback, this);
				// ストリームコールバック関数を登録(指定ファイル再生機能のために常に登録)
				ToggleStreamCallback(true);
				// DWMの更新タイミングでTIMER_FORWARDを呼ぶスレッドを開始(Vista以降)
				if (s_.timerInterval < 0) {
					OSVERSIONINFO vi;
					vi.dwOSVersionInfoSize = sizeof(vi);
					BOOL bEnabled;
					// ここで"dwmapi.dll"を遅延読み込みしていることに注意(つまりXPではDwm*()を踏んではいけない)
					if (GetVersionEx(&vi) && vi.dwMajorVersion >= 6 && SUCCEEDED(DwmIsCompositionEnabled(&bEnabled)) && bEnabled) {
						bQuitSyncThread_ = false;
						hSyncThread_ = reinterpret_cast<HANDLE>(_beginthreadex(NULL, 0, SyncThread, this, 0, NULL));
						if (hSyncThread_) {
							SetThreadPriority(hSyncThread_, THREAD_PRIORITY_ABOVE_NORMAL);
						}
					}
					if (!hSyncThread_) {
						m_pApp->AddLog(L"Aeroが無効のため設定timerIntervalのリフレッシュ同期機能はオフになります。");
						SetTimer(hDummy_, TIMER_FORWARD, 166667 / -s_.timerInterval, NULL);
					}
				}
			}
		}
		return hDummy_ != NULL;
	} else {
		if (hDummy_) {
			if (hSyncThread_) {
				bQuitSyncThread_ = true;
				WaitForSingleObject(hSyncThread_, INFINITE);
				CloseHandle(hSyncThread_);
				hSyncThread_ = NULL;
			}
			ToggleStreamCallback(false);
			m_pApp->SetWindowMessageCallback(NULL);
			DestroyWindow(hDummy_);
			hDummy_ = NULL;
		}
#pragma region TVTComment
		tvtComment = nullptr;
#pragma endregion
		return true;
	}
}

unsigned int __stdcall CViewer::SyncThread(void *pParam)
{
	CViewer *pThis = static_cast<CViewer*>(pParam);
	DWORD count = 0;
	int timeout = 0;
	while (!pThis->bQuitSyncThread_) {
		if (FAILED(DwmFlush())) {
			// ビジーに陥らないように
			Sleep(500);
		}
		if (count >= 10000) {
			// 捌き切れない量のメッセージを送らない
			if (pThis->bPendingTimerForward_ && --timeout >= 0) {
				continue;
			}
			count -= 10000;
			timeout = 30;
			pThis->bPendingTimerForward_ = true;
			SendNotifyMessage(pThis->hDummy_, WM_TIMER, TIMER_FORWARD, 0);
		}
		count += pThis->bHalfSkip_ ? -pThis->s_.timerInterval / 2 : -pThis->s_.timerInterval;
	}
	return 0;
}

void CViewer::ToggleStreamCallback(bool bSet)
{
	if (bSet) {
		if (!bSetStreamCallback_) {
			bSetStreamCallback_ = true;
			pcrPid_ = -1;
			pcrPids_[0] = -1;
			m_pApp->SetStreamCallback(0, StreamCallback, this);
		}
	} else {
		if (bSetStreamCallback_) {
			m_pApp->SetStreamCallback(TVTest::STREAM_CALLBACK_REMOVE, StreamCallback);
			bSetStreamCallback_ = false;
		}
	}
}

void CViewer::LoadFromIni()
{
	// iniはセクション単位で読むと非常に速い。起動時は処理が混み合うのでとくに有利
	TCHAR *pBuf = NewGetPrivateProfileSection(TEXT("Setting"), szIniFileName_);
	s_.timerInterval		= GetBufferedProfileInt(pBuf, TEXT("timerInterval"), -5000);
	s_.halfSkipThreshold	= GetBufferedProfileInt(pBuf, TEXT("halfSkipThreshold"), 9999);
	s_.commentLineMargin	= GetBufferedProfileInt(pBuf, TEXT("commentLineMargin"), 125);
	s_.commentFontOutline	= GetBufferedProfileInt(pBuf, TEXT("commentFontOutline"), 0);
	s_.commentSize			= GetBufferedProfileInt(pBuf, TEXT("commentSize"), 100);
	s_.commentSizeMin		= GetBufferedProfileInt(pBuf, TEXT("commentSizeMin"), 16);
	s_.commentSizeMax		= GetBufferedProfileInt(pBuf, TEXT("commentSizeMax"), 9999);
	GetBufferedProfileString(pBuf, TEXT("commentFontName"), TEXT("ＭＳ Ｐゴシック"), s_.commentFontName, _countof(s_.commentFontName));
	GetBufferedProfileString(pBuf, TEXT("commentFontNameMulti"), TEXT("ＭＳ Ｐゴシック"), s_.commentFontNameMulti, _countof(s_.commentFontNameMulti));
	s_.bCommentFontBold		= GetBufferedProfileInt(pBuf, TEXT("commentFontBold"), 1) != 0;
	s_.bCommentFontAntiAlias = GetBufferedProfileInt(pBuf, TEXT("commentFontAntiAlias"), 1) != 0;
	s_.commentDuration		= GetBufferedProfileInt(pBuf, TEXT("commentDuration"), CCommentWindow::DISPLAY_DURATION);
	s_.commentDrawLineCount = GetBufferedProfileInt(pBuf, TEXT("commentDrawLineCount"), CCommentWindow::DEFAULT_LINE_DRAW_COUNT);
	s_.bUseOsdCompositor	= GetBufferedProfileInt(pBuf, TEXT("useOsdCompositor"), 0) != 0;
	s_.bUseTexture			= GetBufferedProfileInt(pBuf, TEXT("useTexture"), 1) != 0;
	s_.bUseDrawingThread	= GetBufferedProfileInt(pBuf, TEXT("useDrawingThread"), 1) != 0;
	s_.defaultPlaybackDelay	= GetBufferedProfileInt(pBuf, TEXT("defaultPlaybackDelay"), 500);
	delete [] pBuf;
}

HWND CViewer::GetFullscreenWindow()
{
	TVTest::HostInfo hostInfo;
	if (m_pApp->GetFullscreen() && m_pApp->GetHostInfo(&hostInfo)) {
		wchar_t className[64];
		lstrcpynW(className, hostInfo.pszAppName, 48);
		lstrcatW(className, L" Fullscreen");

		HWND hwnd = NULL;
		while ((hwnd = FindWindowExW(NULL, hwnd, className, NULL)) != NULL) {
			DWORD pid;
			GetWindowThreadProcessId(hwnd, &pid);
			if (pid == GetCurrentProcessId()) {
				return hwnd;
			}
		}
	}
	return NULL;
}

static BOOL CALLBACK EnumWindowsProc(HWND hwnd, LPARAM lParam)
{
	void **params = reinterpret_cast<void**>(lParam);
	TCHAR className[64];
	if (GetClassName(hwnd, className, _countof(className)) && !lstrcmp(className, static_cast<LPCTSTR>(params[1]))) {
		// 見つかった
		*static_cast<HWND*>(params[0]) = hwnd;
		return FALSE;
	}
	return TRUE;
}

// TVTestのVideo Containerウィンドウを探す
HWND CViewer::FindVideoContainer()
{
	HWND hwndFound = NULL;
	TVTest::HostInfo hostInfo;
	if (m_pApp->GetHostInfo(&hostInfo)) {
		TCHAR searchName[64];
		lstrcpyn(searchName, hostInfo.pszAppName, 32);
		lstrcat(searchName, L" Video Container");

		void *params[2] = { &hwndFound, searchName };
		HWND hwndFull = GetFullscreenWindow();
		EnumChildWindows(hwndFull ? hwndFull : m_pApp->GetAppWindow(), EnumWindowsProc, reinterpret_cast<LPARAM>(params));
	}
	return hwndFound;
}

// 再生中のストリームのTOT時刻(取得からの経過時間で補正済み)をUTCで取得する
bool CViewer::GetCurrentTot(FILETIME *pft)
{
	CBlockLock lock(&streamLock_);
	DWORD tick = GetTickCount();
	if (ftTot_[0].dwHighDateTime == 0xFFFFFFFF) {
		// TOTを取得できていない
		return false;
	} else if (tick - pcrTick_ >= 2000) {
		// 2秒以上PCRを取得できていない→ポーズ中?
		*pft = ftTot_[0];
		*pft += -s_.defaultPlaybackDelay * FILETIME_MILLISECOND;
		return true;
	} else if (ftTot_[1].dwHighDateTime == 0xFFFFFFFF) {
		// 再生速度は分からない
		*pft = ftTot_[0];
		*pft += ((int)(tick - totTick_[0]) - s_.defaultPlaybackDelay) * FILETIME_MILLISECOND;
		return true;
	} else {
		DWORD delta = totTick_[0] - totTick_[1];
		// 再生速度(10%～1000%)
		LONGLONG speed = !delta ? FILETIME_MILLISECOND : (ftTot_[0] - ftTot_[1]) / delta;
		speed = min(max(speed, FILETIME_MILLISECOND / 10), FILETIME_MILLISECOND * 10);
		*pft = ftTot_[0];
		*pft += ((int)(tick - totTick_[0]) - s_.defaultPlaybackDelay) * speed;
		return true;
	}
}

static int GetWindowHeight(HWND hwnd)
{
	RECT rc;
	return hwnd && GetWindowRect(hwnd, &rc) ? rc.bottom - rc.top : 0;
}

// イベントコールバック関数
// 何かイベントが起きると呼ばれる
LRESULT CALLBACK CViewer::EventCallback(UINT Event, LPARAM lParam1, LPARAM lParam2, void *pClientData)
{
	CViewer *pThis = static_cast<CViewer*>(pClientData);
	switch (Event) {
	case TVTest::EVENT_PLUGINENABLE:
		// プラグインの有効状態が変化した
		return pThis->TogglePlugin(lParam1 != 0);
	case TVTest::EVENT_FULLSCREENCHANGE:
		// 全画面表示状態が変化した
		if (pThis->m_pApp->IsPluginEnabled()) {
			// オーナーが変わるのでコメントウィンドウを作りなおす
			pThis->commentWindow_.Destroy();
			if (pThis->commentWindow_.GetOpacity() != 0 && pThis->m_pApp->GetPreview()) {
				HWND hwnd = pThis->FindVideoContainer();
				pThis->commentWindow_.Create(hwnd);
				pThis->bHalfSkip_ = GetWindowHeight(hwnd) >= pThis->s_.halfSkipThreshold;
			}
		}
		break;
	case TVTest::EVENT_PREVIEWCHANGE:
		// プレビュー表示状態が変化した
		if (pThis->m_pApp->IsPluginEnabled()) {
			if (pThis->commentWindow_.GetOpacity() != 0 && lParam1 != 0) {
				HWND hwnd = pThis->FindVideoContainer();
				pThis->commentWindow_.Create(hwnd);
				pThis->bHalfSkip_ = GetWindowHeight(hwnd) >= pThis->s_.halfSkipThreshold;
			} else {
				pThis->commentWindow_.Destroy();
			}
		}
		break;
	case TVTest::EVENT_CHANNELCHANGE:
		// チャンネルが変更された
		if (pThis->m_pApp->IsPluginEnabled()) {
			PostMessage(pThis->hDummy_, WM_RESET_STREAM, 0, 0);
		}
		// FALL THROUGH!
	case TVTest::EVENT_SERVICECHANGE:
		// サービスが変更された
		if (pThis->m_pApp->IsPluginEnabled()) {
			// 重複やザッピング対策のためタイマで呼ぶ
			SetTimer(pThis->hDummy_, TIMER_SETUP_CURJK, SETUP_CURJK_DELAY, NULL);
		}
		break;
	case TVTest::EVENT_SERVICEUPDATE:
		// サービスの構成が変化した(再生ファイルを切り替えたときなど)
		if (pThis->m_pApp->IsPluginEnabled()) {
			// ユーザの自発的なチャンネル変更(EVENT_CHANNELCHANGE)を捉えるのが原則だが
			// 非チューナ系のBonDriverだとこれでは不十分なため
			SetTimer(pThis->hDummy_, TIMER_SETUP_CURJK, SETUP_CURJK_DELAY, NULL);
		}
		break;
	case TVTest::EVENT_COMMAND:
		// コマンドが選択された
		if (pThis->m_pApp->IsPluginEnabled()) {
			pThis->tvtComment->OnCommandInvoked(static_cast<TVTComment::Command>(lParam1));
		}
		break;
	}
	return 0;
}

BOOL CALLBACK CViewer::WindowMsgCallback(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam, LRESULT *pResult, void *pUserData)
{
	CViewer *pThis = static_cast<CViewer*>(pUserData);
	switch (uMsg) {
	case WM_MOVE:
		pThis->commentWindow_.OnParentMove();
		// 実際に捉えたいVideo Containerウィンドウの変化はすこし遅れるため
		SetTimer(pThis->hDummy_, TIMER_DONE_MOVE, 500, NULL);
		break;
	case WM_SIZE:
		pThis->commentWindow_.OnParentSize();
		SetTimer(pThis->hDummy_, TIMER_DONE_SIZE, 500, NULL);
		break;
	}
	return FALSE;
}

INT_PTR CALLBACK CViewer::ForceDialogProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	if (uMsg == WM_INITDIALOG) {
		SetWindowLongPtr(hwnd, DWLP_USER, lParam);
	}
	CViewer *pThis = reinterpret_cast<CViewer*>(GetWindowLongPtr(hwnd, DWLP_USER));
	return pThis ? pThis->ForceDialogProcMain(hwnd, uMsg, wParam, lParam) : FALSE;
}

INT_PTR CViewer::ForceDialogProcMain(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
#pragma region TVTComment
	if (tvtComment->DialogProc(hwnd, uMsg, wParam, lParam))
		return TRUE;
#pragma endregion

	switch (uMsg) {
	case WM_INITDIALOG:
		commentWindow_.SetStyle(s_.commentFontName, s_.commentFontNameMulti, s_.bCommentFontBold, s_.bCommentFontAntiAlias,
		                        s_.commentFontOutline, s_.bUseOsdCompositor, s_.bUseTexture, s_.bUseDrawingThread);
		commentWindow_.SetCommentSize(s_.commentSize, s_.commentSizeMin, s_.commentSizeMax, s_.commentLineMargin);
		commentWindow_.SetDisplayDuration(s_.commentDuration);
		commentWindow_.SetDrawLineCount(s_.commentDrawLineCount);
		if (commentWindow_.GetOpacity() != 0 && m_pApp->GetPreview()) {
			HWND hwndContainer = FindVideoContainer();
			commentWindow_.Create(hwndContainer);
			bHalfSkip_ = GetWindowHeight(hwndContainer) >= s_.halfSkipThreshold;
		}
		forwardTick_ = timeGetTime();
		forwardOffset_ = 0;
		forwardOffsetDelta_ = 0;
		SendMessage(hwnd, WM_RESET_STREAM, 0, 0);
		if (s_.timerInterval >= 0) {
			SetTimer(hwnd, TIMER_FORWARD, s_.timerInterval, NULL);
		}
		SetTimer(hwnd, TIMER_SETUP_CURJK, SETUP_CURJK_DELAY, NULL);
		// TVTest起動直後はVideo Containerウィンドウの配置が定まっていないようなので再度整える
		SetTimer(hwnd, TIMER_DONE_SIZE, 500, NULL);
		return TRUE;
	case WM_DESTROY:
		{
			commentWindow_.Destroy();
		}
		return FALSE;

#pragma region TVTComment
	case TVTComment::TVTComment::WM_SETCHATOPACITY:
	{
		BYTE newOpacity = (BYTE)wParam;
		if (commentWindow_.GetOpacity() == 0 && newOpacity != 0 && m_pApp->GetPreview()) {
			commentWindow_.ClearChat();
			HWND hwndContainer = FindVideoContainer();
			commentWindow_.Create(hwndContainer);
			bHalfSkip_ = GetWindowHeight(hwndContainer) >= s_.halfSkipThreshold;
		}
		else if (commentWindow_.GetOpacity() != 0 && newOpacity == 0) {
			commentWindow_.Destroy();
		}
		commentWindow_.SetOpacity(newOpacity);
		break;
	}
#pragma endregion


	case WM_TIMER:
		switch (wParam) {
		case TIMER_FORWARD:
			bFlipFlop_ = !bFlipFlop_;
			if (hSyncThread_ || !bHalfSkip_ || bFlipFlop_) {
				bool resyncComment = false;
				{
					CBlockLock lock(&streamLock_);
					if (bResyncComment_) {
						resyncComment = true;
						bResyncComment_ = false;
					}
				}
				// オフセットを調整する
				bool bNotify = false;
				if (0 < forwardOffsetDelta_ && forwardOffsetDelta_ <= 30000) {
					// 前進させて調整
					int delta = min(forwardOffsetDelta_, forwardOffsetDelta_ < 10000 ? 500 : 2000);
					forwardOffset_ += delta;
					forwardOffsetDelta_ -= delta;
					bNotify = forwardOffsetDelta_ == 0;
					commentWindow_.Forward(delta);
				} else if (forwardOffsetDelta_ != 0) {
					// ログファイルを閉じて一気に調整
					forwardOffset_ += forwardOffsetDelta_;
					forwardOffsetDelta_ = 0;
					bNotify = true;
					//ReadFromLogfile(-1);
					commentWindow_.ClearChat();
				} else if (resyncComment) {
					// シーク時のコメント再生位置の再調整
					commentWindow_.ClearChat();
				}
				if (bNotify) {
					TCHAR text[32];
					wsprintf(text, TEXT("(Offset %d)"), forwardOffset_ / 1000);
					commentWindow_.AddChat(text, RGB(0x00,0xFF,0xFF), CCommentWindow::CHAT_POS_UE);
				}
				// コメントの表示を進める
				DWORD tick = timeGetTime();
				commentWindow_.Forward(min(static_cast<int>(tick - forwardTick_), 5000));
				forwardTick_ = tick;
				// 過去ログがあれば処理する
				FILETIME ft;
				if (GetCurrentTot(&ft)) {
					unsigned int tm = FileTimeToUnixTime(ft);
					tm = forwardOffset_ < 0 ? tm - (-forwardOffset_ / 1000) : tm + forwardOffset_ / 1000;

#pragma region TVTComment
					tvtComment->OnForward(tm);
#pragma endregion
					// date属性値は秒精度しかないのでコメント表示が団子にならないよう適当にごまかす
					commentWindow_.ScatterLatestChats(1000);
				}
				commentWindow_.Update();
				bPendingTimerForward_ = false;
			}
			break;
		case TIMER_SETUP_CURJK:
			{
				// 視聴状態が変化したので視聴中のサービスに対応する実況IDを調べて変更する
				KillTimer(hwnd, TIMER_SETUP_CURJK);

#pragma region TVTComment
			tvtComment->OnChannelListChange();
			tvtComment->OnChannelSelectionChange();
#pragma endregion
			}
			break;
		case TIMER_DONE_MOVE:
			KillTimer(hwnd, TIMER_DONE_MOVE);
			commentWindow_.OnParentMove();
			break;
		case TIMER_DONE_SIZE:
			KillTimer(hwnd, TIMER_DONE_SIZE);
			commentWindow_.OnParentSize();
			bHalfSkip_ = GetWindowHeight(FindVideoContainer()) >= s_.halfSkipThreshold;
			break;
		}
		break;
	case WM_RESET_STREAM:
		{
			dprintf(TEXT("CViewer::ForceDialogProcMain() WM_RESET_STREAM\n")); // DEBUG
			CBlockLock lock(&streamLock_);
			ftTot_[0].dwHighDateTime = 0xFFFFFFFF;
		}
		return TRUE;
	}
	return FALSE;
}

// ストリームコールバック(別スレッド)
BOOL CALLBACK CViewer::StreamCallback(BYTE *pData, void *pClientData)
{
	CViewer *pThis = static_cast<CViewer*>(pClientData);
	int pid = ((pData[1]&0x1F)<<8) | pData[2];
	BYTE bTransportError = pData[1]&0x80;
	BYTE bPayloadUnitStart = pData[1]&0x40;
	BYTE bHasAdaptation = pData[3]&0x20;
	BYTE bHasPayload = pData[3]&0x10;
	BYTE bAdaptationLength = pData[4];
	BYTE bPcrFlag = pData[5]&0x10;

	// シークやポーズを検出するためにPCRを調べる
	if (bHasAdaptation && bAdaptationLength >= 5 && bPcrFlag && !bTransportError) {
		DWORD pcr = (static_cast<DWORD>(pData[5+1])<<24) | (pData[5+2]<<16) | (pData[5+3]<<8) | pData[5+4];
		// 参照PIDのPCRが現れることなく5回別のPCRが出現すれば、参照PIDを変更する
		if (pid != pThis->pcrPid_) {
			int i = 0;
			for (; pThis->pcrPids_[i] >= 0; ++i) {
				if (pThis->pcrPids_[i] == pid) {
					if (++pThis->pcrPidCounts_[i] >= 5) {
						pThis->pcrPid_ = pid;
					}
					break;
				}
			}
			if (pThis->pcrPids_[i] < 0 && i + 1 < _countof(pThis->pcrPids_)) {
				pThis->pcrPids_[i] = pid;
				pThis->pcrPidCounts_[i] = 1;
				pThis->pcrPids_[++i] = -1;
			}
		}
		if (pid == pThis->pcrPid_) {
			pThis->pcrPids_[0] = -1;
		}
		CBlockLock lock(&pThis->streamLock_);
		DWORD tick = GetTickCount();
		// 2秒以上PCRを取得できていない→ポーズから回復?
		bool bReset = tick - pThis->pcrTick_ >= 2000;
		pThis->pcrTick_ = tick;
		if (pid == pThis->pcrPid_) {
			long long pcrDiff = static_cast<long long>(pcr) - pThis->pcr_;
			if (pcr < 45000 && (0xFFFFFFFF - 45000) < pThis->pcr_) {
				// ラップアラウンド
				pcrDiff += 0x100000000LL;
			}

			if (0 <= pcrDiff && pcrDiff < 45000) {
				// 1秒以内は通常の再生と見なす
			} else if (abs(pcrDiff) < 15 * 60 * 45000) {
				// -15～0分、+1秒～15分PCRが飛んでいる場合、シークとみなし、
				// シークした分だけTOTをずらして読み込み直す
				if (pThis->ftTot_[0].dwHighDateTime != 0xFFFFFFFF) {
					long long totDiff = pcrDiff * FILETIME_MILLISECOND / 45;
					pThis->ftTot_[0] += totDiff;
					if (pThis->ftTot_[1].dwHighDateTime != 0xFFFFFFFF) {
						pThis->ftTot_[1] += totDiff;
					}
					pThis->bResyncComment_ = true;
				}
			} else {
				// それ以上飛んでたら別ストリームと見なしてリセット
				bReset = true;
			}
			pThis->pcr_ = pcr;
		}
		if (bReset) {
			pThis->ftTot_[0].dwHighDateTime = 0xFFFFFFFF;
			PostMessage(pThis->hDummy_, WM_RESET_STREAM, 0, 0);
		}
	}

	// TOTパケットは地上波の実測で6秒に1個程度
	// ARIB規格では最低30秒に1個
	if (pid == 0x14 && bPayloadUnitStart && bHasPayload && !bTransportError) {
		BYTE *pPayload = pData + 4;
		if (bHasAdaptation) {
			// アダプテーションフィールドをスキップする
			if (bAdaptationLength > 182) {
				pPayload = NULL;
			} else {
				pPayload += 1 + bAdaptationLength;
			}
		}
		if (pPayload) {
			BYTE *pTable = pPayload + 1 + pPayload[0];
			// TOT or TDT (ARIB STD-B10)
			if (pTable + 7 < pData + 188 && (pTable[0] == 0x73 || pTable[0] == 0x70)) {
				// TOT時刻とTickカウントを記録する
				SYSTEMTIME st;
				FILETIME ft;
				if (AribToSystemTime(&pTable[3], &st) && SystemTimeToFileTime(&st, &ft)) {
					// UTCに変換
					ft += -32400000LL * FILETIME_MILLISECOND;
					dprintf(TEXT("CViewer::StreamCallback() TOT\n")); // DEBUG
					CBlockLock lock(&pThis->streamLock_);
					pThis->ftTot_[1] = pThis->ftTot_[0];
					pThis->ftTot_[0] = ft;
					pThis->totTick_[1] = pThis->totTick_[0];
					pThis->totTick_[0] = GetTickCount();
				}
			}
		}
	}
	return TRUE;
}

TVTest::CTVTestPlugin *CreatePluginClass()
{
	return new CViewer();
}
