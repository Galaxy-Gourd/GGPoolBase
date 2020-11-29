namespace GGSharpPool
{
    /// <summary>
    /// Defines pooling behaviors for client objects.
    /// </summary>
    public interface IClientPoolable
    {
        #region Properties

        /// <summary>
        /// True if this instance is NOT being used (according to its pool)
        /// </summary>
        bool availableInPool { get; set; }

        #endregion Properties


        #region Methods

        /// <summary>
        /// When the instance is created by the given pool.
        /// </summary>
        /// <param name="pool">The pool from which the instance was created</param>
        void OnInstanceCreated(PoolBase pool);

        /// <summary>
        /// Claims the instance and activates for use.
        /// </summary>
        void Claim();
        
        /// <summary>
        /// Releases ownership and returns instance to the pool for later use.
        /// </summary>
        void Relinquish();

        /// <summary>
        /// Recycles an instance (immediately reusing in different context).
        /// </summary>
        void Recycle();

        /// <summary>
        /// Removes the instance from the pool entirely.
        /// </summary>
        void DeleteFromPool();

        #endregion Methods
    }
}