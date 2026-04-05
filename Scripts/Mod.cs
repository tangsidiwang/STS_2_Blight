using Godot.Bridge;
using BlightMod.Cards;
using BlightMod.Core;
using BlightMod.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using System;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.Models;

namespace Test.Scripts;

// 必须要加的属性，用于注册Mod。字符串和初始化函数命名一致�?
[ModInitializer("Init")]
public class Entry
{
    // 初始化函�?
    public static void Init()
    {
        BlightSaveLoadWindow.EnsureInitialized();
        ModHelper.AddModelToPool<SharedRelicPool, BlightArmorRelic>();
        ModHelper.AddModelToPool<StatusCardPool, BlightSandSpear>();

        // 打patch（即修改游戏代码的功能）�?
        // 传入参数随意，只要不和其他人撞车即可
        var harmony = new Harmony("sts2.tongs.blight");
        harmony.PatchAll();
        MegaCrit.Sts2.Core.Logging.Log.Info("[BLIGHT PATCH] PatchAll 已调用，补丁已加载");

        // ...existing code...
        // 使得tscn可以加载自定义脚�?
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);
        Log.Info("BLIGHT MOD LOADED SUCCESSFULLY!"); Log.Debug("Mod initialized!");
    }
}
