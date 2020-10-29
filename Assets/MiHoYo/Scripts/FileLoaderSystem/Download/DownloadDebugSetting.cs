using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class DownloadDebugSetting
{
    private const string SimulateConfigKeyWordsInEditor = "PBStaticConfigReadDebugSetting::SimulateInEditor2";

    public static bool IsConfigSimulationMode
    {
        get { return EditorPrefs.GetBool(SimulateConfigKeyWordsInEditor, true); }

        set
        {
            EditorPrefs.SetBool(SimulateConfigKeyWordsInEditor,
                !EditorPrefs.GetBool(SimulateConfigKeyWordsInEditor));
        }
    }


    private const string SimulateResAsbKeyWordsInEditor = "ResAsbReadDebugSetting::SimulateInEditor";

    public static bool IsResAsbSimulationMode
    {
        get { return EditorPrefs.GetBool(SimulateResAsbKeyWordsInEditor, true); }

        set
        {
            EditorPrefs.SetBool(SimulateResAsbKeyWordsInEditor,
                !EditorPrefs.GetBool(SimulateResAsbKeyWordsInEditor));
        }
    }

    private const string SimulateResPckInEditor = "ResPckReadDebugSetting::SimulateInEditor";

	///<summary>true 使用 本地Pck 资源。false 表示音频使用本地的 bnk 资源。</summary>
    public static bool IsResPckSimulationMode
    {
        get { return EditorPrefs.GetBool(SimulateResPckInEditor, true); }

        set
        {
            EditorPrefs.SetBool(SimulateResPckInEditor,
                !EditorPrefs.GetBool(SimulateResPckInEditor));
        }
    }	

    
    private const string SimulateInPackageSimulationMode = "ResPckReadDebugSetting::InPackageSimulationMode";

    ///<summary>true 模拟首包资源模式。</summary>
    public static bool IsInPackageSimulationMode
    {
        get { return EditorPrefs.GetBool(SimulateInPackageSimulationMode, false); }

        set
        {
            EditorPrefs.SetBool(SimulateInPackageSimulationMode,
                !EditorPrefs.GetBool(SimulateInPackageSimulationMode));
        }
    }

    private const string AssetBundleMode = "ResReadDebugSetting::AssetBundleMode";

    ///<summary>true 资源Bundle模式。</summary>
    public static bool IsInAssetBundleMode
    {
        get { return EditorPrefs.GetBool(AssetBundleMode, false); }

        set
        {
            EditorPrefs.SetBool(AssetBundleMode,
                !EditorPrefs.GetBool(AssetBundleMode));
        }
    }
}

#endif