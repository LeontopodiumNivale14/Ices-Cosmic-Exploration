using System;
using System.Collections.Generic;
using System.Text;

namespace ICE.ConfigFiles;

public partial class Config
{
    public bool ShowInfoButton { get; set; } = true;
    public float MiddleColumnWidth { get; set; } = 1000f;
    public uint SelectedJob { get; set; } = 8;
    public bool XPRelicGrind { get; set; } = false;
    public bool XPLeveling_Mode { get; set; } = false;
    public bool XPRelicIgnoreManual { get; set; } = false;
    public bool XPRelicOnlyEnabled { get; set; } = false;
    public bool ShowCritical { get; set; } = true;
    public bool ShowSequential { get; set; } = true;
    public bool ShowWeather { get; set; } = true;
    public bool ShowTimeRestricted { get; set; } = true;
    public bool ShowClassA { get; set; } = true;
    public bool ShowClassB { get; set; } = true;
    public bool ShowClassC { get; set; } = true;
    public bool ShowClassD { get; set; } = true;
}
