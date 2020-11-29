namespace GGSharpPool
{
    /// <summary>
    /// Defines the public requirements for a pool object.
    /// </summary>
    public interface IPool
    {
        #region Active Properties

        /// <summary>
        /// The minimum number of pooled instances for this pool. If the number of items is lower than this
        /// value when it is set, the required number of items will be created to meet the minimum.
        /// </summary>
        int capacityMin { get; set; }
        
        /// <summary>
        /// The maximum number of items this pool can contain. When this threshold is reached, the pool will
        /// begin recycling instances rather than creating them anew, unless @spilloverAllowance > 0.
        /// </summary>
        int capacityMax { get; set; }
        
        /// <summary>
        /// Defines the behavior for the pool when the maximum instance count has been met and a new instance
        /// is requested. Only relevant when pool has a maximum instance count defined. Spillover instances are
        /// destroyed when they are relinquished, rather than being returned to the pool.
        /// </summary>
        int spilloverAllowance { get; set; }
        
        #endregion Active Properties


        #region Passive Properties

        /// <summary>
        /// The total number of instances this pool currently contains.
        /// </summary>
        int instanceCount { get; }
    
        /// <summary>
        /// The total number of ACTIVE instances this pool currently contains.
        /// </summary>
        int activeCount { get; }
        
        /// <summary>
        /// The number of times instances in the pool have been recycled for reuse.
        /// </summary>
        int recyclesCount { get; }

        /// <summary>
        /// Returns the number of instances in the pool that are spillover.
        /// </summary>
        int activeSpilloverCount { get; }
        
        /// <summary>
        /// The number of times instances have been used from the pool - either available, or recycled
        /// </summary>
        int pooledUseCount { get; }
        
        /// <summary>
        /// The label for this pool; used for debugging.
        /// </summary>
        string poolLabel { get; set; }

        #endregion Passive Properties


        #region Methods

        /// <summary>
        /// Claims and returns the next available instance from the pool.
        /// </summary>
        IClientPoolable GetNext();

        /// <summary>
        /// Manually claims a specific pooled instance.
        /// </summary>
        void ClaimInstance(IClientPoolable instance, bool isNewInstance = false);

        /// <summary>
        /// Manually relinquishes a specific pooled instance.
        /// </summary>
        void RelinquishInstance(IClientPoolable instance);

        /// <summary>
        /// When an instance deletes itself, the pool needs to know about it.
        /// </summary>
        /// <param name="instance"></param>
        void DeleteFromInstance(IClientPoolable instance);

        /// <summary>
        /// Destroys any available instances remaining in the pool.
        /// </summary>
        void Clean();
        
        /// <summary>
        /// Clears all instance from the pool and resets to empty state.
        /// </summary>
        void Clear();

        #endregion
    }
}