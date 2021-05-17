using GG.Data.Base;

namespace GG.Pool.Base
{
    public class DataConfigPool : DataConfig
    {
        public string Label;
        public int MinimumCapacity;
        public int MaximumCapacity;
        public int SpilloverAllowance;
    }
}