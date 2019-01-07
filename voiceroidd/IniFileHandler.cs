using System.Text;
using System.Runtime.InteropServices;

class IniFileHandler
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern uint GetPrivateProfileString(
        string lpAppName,
        string lpKeyName, 
        string lpDefault, 
        StringBuilder lpReturnedString, 
        uint nSize,
        string lpFileName);
    
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern uint WritePrivateProfileString(
        string lpAppName,
        string lpKeyName,
        string lpString, 
        string lpFileName);
};
