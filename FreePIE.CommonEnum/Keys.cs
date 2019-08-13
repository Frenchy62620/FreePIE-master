using FreePIE.Core.Contracts;

namespace FreePIE.CommonEnum
{
    [GlobalEnum]
    public enum Key
    {
        Escape = 1,
        D1 = 2,
        D2,
        D3,
        D4,
        D5,
        D6,
        D7,
        D8,
        D9 = 10,
        D0 = 11,
        Minus = 12,
        Equals = 13,
        Back,
        Tab,

        E = 18,
        R,
        T,
        Y,
        U,
        I,
        O,
        P = 25,

        Return = 28, Enter = Return,
        LeftControl, LCtrl = LeftControl,

        S = 31,
        D,
        F,
        G,
        H,
        J,
        K,
        L = 38,

        Grave = 41,
        LeftShift, LShft = LeftShift,

        X = 45,
        C,
        V,
        B,
        N = 49,

        RightShift = 54, RShft = RightShift,
        NumberPadStar, // = Multiply,
        LeftAlt,
        LAlt = LeftAlt,
        Space,
        Capital,
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10,
        NumberLock,
        ScrollLock,
        NumberPad7, NumberPad8, NumberPad9, NumberPadMinus, // = Subtract,
        NumberPad4, NumberPad5, NumberPad6, NumberPadPlus, // = Add,
        NumberPad1, NumberPad2, NumberPad3,
        NumberPad0, NumberPadPeriod, // = Decimal,
        Oem102 = 86,
        F11, F12,
        F13 = 100, F14, F15,
        Kana = 112,
        AbntC1 = 115,
        Convert = 121,
        NoConvert = 123,
        Yen = 125,
        AbntC2,
        NumberPadEquals = 141,
        PreviousTrack = 144,
        AT,

        Underline = 147, //sup Key
        Kanji = 148, // sup key
        Stop = 149, // sup key
        AX,
        Unlabeled,
        NextTrack = 153,
        NumberPadEnter = 156,
        RightControl, RCtrl = RightControl,
        Mute = 160,
        Calculator,
        PlayPause,
        MediaStop = 164,
        VolumeDown = 174,
        VolumeUp = 176,
        WebHome = 178,
        NumberPadComma,
        NumberPadSlash = 181, // = Divide,
        PrintScreen = 183,
        RightAlt, RAlt = RightAlt,
        Pause = 197,
        Home = 199,
        UpArrow, //Up
        PageUp,
        LeftArrow = 203, //Left
        RightArrow = 205, //Right
        End = 207,
        DownArrow, //Down
        PageDown,
        Insert,
        Delete,
        LeftWindowsKey = 219, LWKey = LeftWindowsKey,
        RightWindowsKey, RWKey = RightWindowsKey,
        Applications,
        Power,
        Sleep,
        Wake = 227,
        WebSearch = 229,
        WebFavorites,
        WebRefresh,
        WebStop,
        WebForward,
        WebBack,
        MyComputer,
        Mail,
        MediaSelect,
        LastKey = MediaSelect,
        Unknown = 0,

#if (AZERTY)
        A = 16,
        Z,
        Circonflex = 26,
        Dollar,
        Q = 30,
        M = 39,
        Uaccent,
        Star = 43,
        W,
        Comma = 50,
        Semicolon,
        Colon,
        Exclamation,
        //ColonX = 146, //unused
#else
        Q = 16,
        W,
        LeftBracket = 26,
        RightBracket,
        A = 30,
        Semicolon = 39,
        Apostrophe,
        Backslash = 43,
        Z,
        M = 50,
        Comma,
        Period,
        Slash,
        Colon = 146,
#endif

    }

    [GlobalEnum]
    public enum Mouse
    {
        Unknown = 0,
        Left = 238,
        Right,
        Middle,
        X1,
        X2,
        WheelFwd,
        WheelBwd, LastButton = WheelBwd,
        XY = 64 * 8
    }

    [GlobalEnum]
    public enum HOOK
    {
        STOP_PROCESS = 0,
        KEYBOARD = 245,
        BUFFERED = 246,
        GETDATAFLAG = 247,
        DATA = 248,
        MOUSE = 256,
        BOTH = KEYBOARD + MOUSE
    }
}
