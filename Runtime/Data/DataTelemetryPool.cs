using GGSharpData;

namespace GGSharpPool
{
    public class DataTelemetryPool : DataTelemetry
    {
        #region DATA

        /// <summary>
        /// The total number of instances this pool currently contains.
        /// </summary>
        public int InstanceCount;

        /// <summary>
        /// The total number of ACTIVE instances this pool currently contains.
        /// </summary>
        public int ActiveCount;

        /// <summary>
        /// The number of times instances in the pool have been recycled for reuse.
        /// </summary>
        public int RecyclesCount;

        /// <summary>
        /// Returns the number of instances in the pool that are spillover.
        /// </summary>
        public int ActiveSpilloverCount;

        /// <summary>
        /// The number of times instances have been used from the pool - either available, or recycled
        /// </summary>
        public int PooledUseCount;

        #endregion DATA
    }
}