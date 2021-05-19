using GG.Data.Base;

namespace GGPoolBase
{
    public class DataConfigPool : DataConfig
    {
        public string Label;
        public int MinimumCapacity;
        public int MaximumCapacity;
        public int SpilloverAllowance;
    }
}