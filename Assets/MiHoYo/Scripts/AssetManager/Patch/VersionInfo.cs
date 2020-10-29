namespace NewWarMap.Patch
{
    public class VersionInfo
    {
        public int MarjorVersion;
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
            return $"{MarjorVersion}.{MinorVersion}.{BuildVersion}";
        }

        public State CheckUpdateState(FileMapSystem.Version current)
        {
            if (MarjorVersion != current.Version_Major)
            {
                return State.MustDownloadAppAgain;
            }

            return current.Version_Build < BuildVersion ? State.NeedUpdate : State.NotNeedUpdate;
        }
    }
}