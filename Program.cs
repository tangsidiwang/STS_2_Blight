using System;
using System.Reflection;
using System.Linq;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

class Program {
    static void Main() {
        var asm = Assembly.LoadFrom("Z:\\SteamLibrary\\steamapps\\common\\Slay the Spire 2\\data_sts2_windows_x86_64\\sts2.dll");
        var type = asm.GetType("MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NSingleplayerSubmenu");
        if(type != null) {
            foreach(var m in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                Console.WriteLine(m.Name + " -> " + m.ReturnType.Name);
            }
        }
    }
}
