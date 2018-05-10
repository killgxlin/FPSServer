namespace FPSServer.XmlConfig
{
    internal class XMLConfigMgr
    {
        private string resPath;

        public bool Init(string resPath)
        {
            this.resPath = resPath;
            return false;
        }

        public void Destroy()
        {
        }
    }
}