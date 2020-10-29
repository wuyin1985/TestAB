namespace NewWarMap.Patch
{
    public class VersionInfo
    {
        public int MajorVersion;
        public int MinorVersion;
        public int BuildVersion;
        public string AppUpdateUrl;

        public enum State
        {
            NotNeedUpdate,
            NeedUpdate,
            MustDownloadAppAgain,
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