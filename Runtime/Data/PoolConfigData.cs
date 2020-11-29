namespace GGSharpPool
{
    /// <summary>
    /// Defines configuration data for a pool.
    /// </summary>
    public class PoolConfigData
    {
        public int minimumCapacity;
        public int maximumCapacity;
        public int spilloverAllowance;
        public string label;
    }
}