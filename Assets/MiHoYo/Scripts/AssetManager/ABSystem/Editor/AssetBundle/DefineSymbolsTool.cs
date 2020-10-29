using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class DefineSymbolsTool
{
    //[MenuItem("Tools/SetDefineTest", false, 10)]
    //public static void SetDefineTest()
    //{
    //    SetDefineSymbols(BuildTargetGroup.Standalone, "");
    //}

    public static void SetDefineSymbols(BuildTargetGroup target, string defines)
    {
        //OS.System参数中含有分号;会被截断成多条指令
        defines = defines.Replace("#", ";");
        CommonLog.Log($"SetDeinfeSymbols={defines}");
        PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
    }

    public static void AddDefineSymbol(BuildTargetGroup target, string define)
    {
        var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
        var defineArray = defineSymbols.Split(';');

        bool isHasDefine = false;
        for (int i = 0; i < defineArray.Length; i++)
        {
            if (defineArray[i].Equals(define))
            {
                isHasDefine = true;
                break;
            }
        }

        if (!isHasDefine)
        {
            defineSymbols += $";{define}";
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defineSymbols);
        }
    }

    public static void RemoveDefineSymbol(BuildTargetGroup target, string define)
    {
        var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
        var defineArray = defineSymbols.Split(';');

        bool isHasDefine = false;
        for (int i = 0; i < defineArray.Length; i++)
        {
            if (defineArray[i].Equals(define))
            {
                isHasDefine = true;
                break;
            }
        }

        if (isHasDefine)
        {
            var newDefineSymbols = defineSymbols.Replace($";{define}", string.Empty)
                                                .Replace($"{define};", string.Empty)
                                                .Replace($"{define}", string.Empty);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, newDefineSymbols);
        }
    }
}
