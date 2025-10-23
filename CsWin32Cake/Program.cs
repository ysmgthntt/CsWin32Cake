// Original Code: https://www.charlespetzold.com/
/*-----------------------------------------------------
   CAKE.C programmed by Charles Petzold, November 1985
  -----------------------------------------------------*/

using System.Drawing;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

static unsafe class Program
{
    const int FALSE = 0;
    const int TRUE = 1;

    const SHOW_WINDOW_CMD SHOW_FULLSCREEN = (SHOW_WINDOW_CMD)3;

    const nuint IDOK = 1;
    const nuint IDCANCEL = 2;

    // CAKE.H

    const nuint IDM_SETTINGS = 1;
    const nuint IDM_BURNING = 2;

    const int ID_CAKETEXT = 101;
    const int ID_NUMCANDLES = 102;
    const int ID_FLASH = 103;

    //

    const string szAppName = "Cake";
    static string strCake = "<text>";
    static int cCandles = 1;
    static BOOL bFlash = false;
    static HINSTANCE hInst;

    static int Main(string[] args)
    {
        HINSTANCE hInstance = (HINSTANCE)GetModuleHandle((PCWSTR)null);

        HWND hwnd;
        MSG msg;
        WNDCLASSW wndclass;

        hInst = hInstance;

        fixed (char* lpszAppName = szAppName)
        fixed (char* lpWindowName = "Birthday Cake")
        {
            wndclass.style = WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW;
            wndclass.lpfnWndProc = WndProc;
            wndclass.cbClsExtra = 0;
            wndclass.cbWndExtra = 0;
            wndclass.hInstance = hInstance;
            wndclass.hIcon = LoadIcon(HINSTANCE.Null, IDI_APPLICATION);
            wndclass.hCursor = LoadCursor(HINSTANCE.Null, IDC_ARROW);
            wndclass.hbrBackground = (HBRUSH)GetStockObject(GET_STOCK_OBJECT_FLAGS.BLACK_BRUSH).Value;
            wndclass.lpszMenuName = lpszAppName;
            wndclass.lpszClassName = lpszAppName;

            if (!(BOOL)RegisterClass(wndclass))
                return FALSE;

            hwnd = CreateWindowEx(0, lpszAppName, lpWindowName,
                                  WINDOW_STYLE.WS_TILEDWINDOW, 0, 0, 0, 0,
                                  HWND.Null, HMENU.Null, hInstance, null);
        }

        ShowWindow(hwnd, SHOW_FULLSCREEN);
        UpdateWindow(hwnd);

        while (GetMessage(out msg, HWND.Null, 0, 0))
        {
            TranslateMessage(msg);
            DispatchMessage(msg);
        }
        return (int)msg.wParam.Value;
    }


    static readonly COLORREF clrIcing = RGB(0xFF, 0xC0, 0xC0);
    static readonly Point pt1 = new(40, 125);
    static readonly Point pt2 = new(600, 275);
    static readonly short ht = 50;
    static readonly COLORREF[] clrCandle = [ RGB(0xFF, 0, 0),
                                             RGB(0, 0xFF, 0),
                                             RGB(0, 0, 0xFF) ];
    static DLGPROC lpSettingsProc = null!;

    static HBITMAP[] hbmFlame = new HBITMAP[3];
    static int iTextColor;

    static LRESULT WndProc(HWND hwnd, uint iMessage, WPARAM wParam, LPARAM lParam)
    {
        HPEN hpen;
        HDC hdc, hdcMem;
        int i;
        PAINTSTRUCT ps;
        RECT rect;

        switch (iMessage)
        {
            case WM_CREATE:
                lpSettingsProc = SettingsProc;

                hdc = GetDC(hwnd);
                for (i = 0; i < 3; i++)
                    hbmFlame[i] = CreateFlameBitmap(hdc, i);
                ReleaseDC(hwnd, hdc);

                SetFocus(hwnd);
                break;

            case WM_COMMAND:
                switch ((nuint)wParam)
                {
                    case IDM_SETTINGS:
                        if ((BOOL)DialogBox(hInst, "SettingsBox", /* MAKEINTRESOURCE(IDD_SETTINGS), */
                                            hwnd, lpSettingsProc))
                        {
                            if (bFlash)
                            {
                                SetTimer(hwnd, 2, 250, null);
                                SetTimer(hwnd, 3, 1, null);
                            }
                            else
                            {
                                KillTimer(hwnd, 2);
                                KillTimer(hwnd, 3);
                            }

                            InvalidateRect(hwnd, null, true);
                        }
                        break;

                    case IDM_BURNING:
                        SetTimer(hwnd, 1, 1, null);
                        break;
                }
                break;

            case WM_PAINT:
                hdc = BeginPaint(hwnd, out ps);

                SelectObject(hdc, CreateSolidBrush(clrIcing));

                Ellipse(hdc, (short)pt1.X, (short)pt2.Y - ht / 2,
                             (short)pt2.X, (short)pt2.Y + ht / 2);

                hpen = (HPEN)SelectObject(hdc, GetStockObject(GET_STOCK_OBJECT_FLAGS.NULL_PEN)).Value;

                Rectangle(hdc, (short)pt1.X, (short)pt1.Y,
                               (short)pt2.X, (short)pt2.Y);

                SelectObject(hdc, (HGDIOBJ)hpen);

                Ellipse(hdc, (short)pt1.X, (short)pt1.Y - ht / 2,
                             (short)pt2.X, (short)pt1.Y + ht / 2);

                DeleteObject(SelectObject(hdc, GetStockObject(GET_STOCK_OBJECT_FLAGS.BLACK_BRUSH)));

                SelectObject(hdc, GetStockObject(GET_STOCK_OBJECT_FLAGS.BLACK_PEN));

                MoveToEx(hdc, (short)pt1.X, (short)pt1.Y, null);
                LineTo(hdc, (short)pt1.X, (short)pt2.Y);
                MoveToEx(hdc, (short)pt2.X, (short)pt1.Y, null);
                LineTo(hdc, (short)pt2.X, (short)pt2.Y);

                DisplayText(hdc, pt1, pt2, ht);

                for (i = 0; i < cCandles; i++)
                {
                    short x = (short)(((cCandles - i) * pt1.X + (i + 1) * pt2.X) / (cCandles + 1));
                    short y = (short)pt1.Y;

                    if (i % 2 == 1)
                        y -= 5;
                    else
                        y += 5;

                    Candle(hdc, clrCandle[i % 3], x, y);
                }

                EndPaint(hwnd, ps);
                break;

            case WM_TIMER:
                hdc = GetDC(hwnd);

                switch ((nuint)wParam)
                {
                    case 1:
                        hdcMem = CreateCompatibleDC(hdc);

                        for (i = 0; i < cCandles; i++)
                        {
                            short x = (short)(((cCandles - i) * pt1.X + (i + 1) * pt2.X) / (cCandles + 1));
                            short y = (short)pt1.Y;

                            if (i % 2 == 1)
                                y -= 5;
                            else
                                y += 5;

                            SelectObject(hdcMem, hbmFlame[rand() % 3]);
                            BitBlt(hdc, x - 7, y - 115, 15, 25, hdcMem, 0, 0, ROP_CODE.SRCCOPY);
                        }
                        DeleteDC(hdcMem);

                        break;

                    case 2:
                        SetTextColor(hdc, (BOOL)(iTextColor % 4) ? RGB(0, 0, 0) : RGB(255, 0, 0));
                        DisplayText(hdc, pt1, pt2, ht);
                        iTextColor++;
                        break;

                    case 3:
                        Firework(hdc);
                        break;
                }
                ReleaseDC(hwnd, hdc);
                break;

            case WM_DESTROY:
                PostQuitMessage(0);
                break;

            default:
                return DefWindowProc(hwnd, iMessage, wParam, lParam);
        }
        return (LRESULT)0;
    }

    static nint SettingsProc(HWND hdlg, uint message, WPARAM wParam, LPARAM lParam)
    {
        switch (message)
        {
            case WM_INITDIALOG:
                SetDlgItemText(hdlg, ID_CAKETEXT, strCake);
                SetDlgItemInt(hdlg, ID_NUMCANDLES, (uint)cCandles, false);
                SendDlgItemMessage(hdlg, ID_FLASH, BM_SETCHECK, (nuint)bFlash.Value, 0);
                return TRUE;

            case WM_COMMAND:
                switch ((nuint)wParam)
                {
                    case IDOK:
                        char* buffer = stackalloc char[80];
                        GetDlgItemText(hdlg, ID_CAKETEXT, buffer, 80);
                        strCake = new string(buffer);
                        cCandles = (int)GetDlgItemInt(hdlg, ID_NUMCANDLES, null, false);
                        bFlash = (BOOL)SendDlgItemMessage(hdlg, ID_FLASH, BM_GETCHECK, 0, 0).Value;
                        EndDialog(hdlg, TRUE);
                        return TRUE;

                    case IDCANCEL:
                        EndDialog(hdlg, FALSE);
                        return TRUE;
                }
                break;

        }
        return FALSE;
    }

    static void Candle(HDC hdc, COLORREF clr, short x, short y)
    {
        short cx = 10;
        short cy = 75;
        short h = 4;
        short wick = 15;
        HBRUSH hbr = CreateSolidBrush(clr);

        SelectObject(hdc, hbr);
        Rectangle(hdc, x - cx / 2, y - cy, x + cx / 2, y);

        SelectObject(hdc, GetStockObject(GET_STOCK_OBJECT_FLAGS.WHITE_PEN));
        SelectObject(hdc, GetStockObject(GET_STOCK_OBJECT_FLAGS.WHITE_BRUSH));
        Rectangle(hdc, x - 2, y - cy, x + 2, y - cy - 16);

        SelectObject(hdc, GetStockObject(GET_STOCK_OBJECT_FLAGS.BLACK_PEN));
        DeleteObject(hbr);
    }


    static int[] pos = [0, 7, 15];
    static Point[] apt = [new(0, 0), new(0, 15), new(15, 15)];

    static int[] pos2 = [3, 7, 11];
    static Point[] aptInner = [new(7, 10), new(3, 17), new(7, 20), new(11, 17)];

    static HBITMAP CreateFlameBitmap(HDC hdc, int i)
    {
        RECT rect;
        HBITMAP hbm = CreateCompatibleBitmap(hdc, 15, 25);
        HDC hdcMem = CreateCompatibleDC(hdc);
        SelectObject(hdcMem, hbm);

        SetRect(out rect, 0, 0, 15, 25);
        FillRect(hdcMem, &rect, (HBRUSH)GetStockObject(GET_STOCK_OBJECT_FLAGS.BLACK_BRUSH).Value);

        SelectObject(hdcMem, CreateSolidBrush(RGB(0xFF, 0xFF, 0)));
        SelectObject(hdcMem, GetStockObject(GET_STOCK_OBJECT_FLAGS.NULL_PEN));
        Ellipse(hdcMem, 0, 10, 15, 25);

        apt[0].X = pos[i];
        aptInner[0].X = pos2[i];

        Polygon(hdcMem, apt);
        DeleteObject(SelectObject(hdcMem, CreateSolidBrush(RGB(0xFF, 0, 0))));
        Polygon(hdcMem, aptInner);
        DeleteObject(SelectObject(hdcMem, GetStockObject(GET_STOCK_OBJECT_FLAGS.WHITE_BRUSH)));

        DeleteDC(hdcMem);
        return hbm;
    }


    static short xOrig, yOrig, iStep = 0;
    static short[] x = new short[8];
    static short[] y = new short[8];
    static short[] xInc = [-1, 0, 1, -1, 1, -1, 0, 1];
    static short[] yInc = [-1, -1, -1, 0, 0, 1, 1, 1];
    static COLORREF[] clr = new COLORREF[8];
    static COLORREF Black = RGB(0, 0, 0);
    static COLORREF White = RGB(255, 255, 255);

    static void Firework(HDC hdc)
    {
        int i;

        if (iStep % 6 == 0)
        {
            xOrig = (short)(rand() % 640);
            yOrig = (short)(rand() % 125);
            iStep = 0;

            for (i = 0; i < 8; i++)
            {
                x[i] = (short)(xOrig + xInc[i]);
                y[i] = (short)(yOrig + yInc[i]);
            }
        }
        else
        {
            for (i = 0; i < 8; i++)
            {
                SetPixel(hdc, x[i], y[i], clr[i]);
                x[i] += (short)(xInc[i] * iStep);
                y[i] += (short)(yInc[i] * iStep);
            }
        }

        if (iStep % 6 != 5)
            for (i = 0; i < 8; i++)
            {
                clr[i] = GetPixel(hdc, x[i], y[i]);
                SetPixel(hdc, x[i], y[i], White);
            }
        iStep++;
    }

    static void DisplayText(HDC hdc, Point pt1, Point pt2, short ht)
    {
        RECT rect;
        HFONT hFont;
        fixed (char* pszFaceName = "Script")
            hFont = CreateFont(67, 0, 0, 0, 700, 0, 0, 0, (uint)FONT_CHARSET.OEM_CHARSET,
                               0, 0, 0, 0, pszFaceName);

        SelectObject(hdc, hFont);
        SetRect(out rect, pt1.X + 50, pt1.Y + ht / 2, pt2.X - 50, pt2.Y);

        SetBkMode(hdc, BACKGROUND_MODE.TRANSPARENT);
        fixed (char* lpchText = strCake)
            DrawText(hdc, lpchText, strCake.Length, ref rect,
                     DRAW_TEXT_FORMAT.DT_CENTER | DRAW_TEXT_FORMAT.DT_VCENTER | DRAW_TEXT_FORMAT.DT_WORDBREAK);

        OffsetRect(ref rect, 1, 1);

        fixed (char* lpchText = strCake)
            DrawText(hdc, lpchText, strCake.Length, ref rect,
                     DRAW_TEXT_FORMAT.DT_CENTER | DRAW_TEXT_FORMAT.DT_VCENTER | DRAW_TEXT_FORMAT.DT_WORDBREAK);

        SetBkMode(hdc, BACKGROUND_MODE.OPAQUE);
        DeleteObject(SelectObject(hdc, GetStockObject(GET_STOCK_OBJECT_FLAGS.SYSTEM_FONT)));
    }

    //

    static int rand() => System.Random.Shared.Next();

    static COLORREF RGB(uint r, uint g, uint b) => (COLORREF)(r | g << 8 | b << 16);

    static nint DialogBox(HINSTANCE hInstance, string templateName, HWND hWndParent, DLGPROC lpDialogFunc)
    {
        fixed(char* lpTemplateName = templateName)
        {
            return DialogBoxParam(hInstance, lpTemplateName, hWndParent, lpDialogFunc, 0);
        }
    }

    [DllImport("GDI32.dll", ExactSpelling = true, EntryPoint = "CreateFontW"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    static extern HFONT CreateFont(int cHeight, int cWidth, int cEscapement, int cOrientation, int cWeight, uint bItalic, uint bUnderline, uint bStrikeOut, uint iCharSet, uint iOutPrecision, uint iClipPrecision, uint iQuality, uint iPitchAndFamily, PCWSTR pszFaceName);
}
