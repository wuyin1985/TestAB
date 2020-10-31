using System;
using UnityEngine;

namespace NewWarMap.Patch
{
    public class VersionInfo
    {
        public int MajorVersion;
        public int MinorVersion;
        public int BuildVersion;
        public string AppUpdateUrl;

        public long AssetBundleXmlMd51;
        public long AssetBundleXmlMd52;

        public long AssetBundlesCacheXmfMd51;
        public long AssetBundlesCacheXmfMd52;

        public enum State
        {
            NotNeedUpdate,
            NeedUpdate,
            MustDownloadAppAgain,
        }

        public bool CheckMd5Valid()
        {
            if (AssetBundleXmlMd51 == 0 && AssetBundleXmlMd52 == 0)
            {
                CommonLog.Error("AssetBundleXmlMd5 is zero , not valid");
                return false;
            }

            if (AssetBundlesCacheXmfMd51 == 0 && AssetBundlesCacheXmfMd52 == 0)
            {
                CommonLog.Log("AssetBundlesCacheXmfMd5 is zero , not valid");
                return false;
            }

            return true;
        }

        public string DumpVersion()
        {
            return $"{MajorVersion}.{MinorVersion}.{BuildVersion}";
        }

        public State CheckUpdateState(FileMapSystem.Version current)
        {
            if (MajorVersion != current.Version_Major)
            {
                return State.MustDownloadAppAgain;
            }

            return current.Version_Build < BuildVersion ? State.NeedUpdate : State.NotNeedUpdate;
        }
    }
}