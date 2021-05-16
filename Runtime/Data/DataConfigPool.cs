using GGSharpData;

namespace GGSharpPool
{
    public class DataConfigPool : DataConfig
    {
        public string Label;
        public int MinimumCapacity;
        public int MaximumCapacity;
        public int SpilloverAllowance;
    }
}