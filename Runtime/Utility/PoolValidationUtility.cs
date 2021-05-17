using System;

namespace GG.Pool.Base
{
    /// <summary>
    /// Validates various pool data/parameters.
    /// </summary>
    internal static class PoolValidationUtility
    {
        /// <summary>
        /// 
        /// </summary>
        internal static bool ValidatePoolCapacity(int min, int max)
        {
            if (max == 0)
                throw new ArgumentException("Pool max capacity must not be set to 0!");
            
            if (max > 0 && min > max)
                throw new ArgumentException("Pool min capacity must not be greater than non-infinite max!");

            return true;
        }
    }
}