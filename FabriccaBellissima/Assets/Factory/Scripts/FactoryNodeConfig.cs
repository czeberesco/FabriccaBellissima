using System;

namespace FabriccaBellissima.Factory
{
    [Serializable]
    public abstract class FactoryNodeConfig
    {
        public int configurationVersion = 1;
        public int nodeId;
        public string displayName = string.Empty;
        public FactoryNodeKind kind;
        public bool enabled = true;

        protected FactoryNodeConfig(FactoryNodeKind kind)
        {
            this.kind = kind;
        }
    }
}
