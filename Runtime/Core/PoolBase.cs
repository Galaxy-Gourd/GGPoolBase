using System;
using System.Collections.Generic;

namespace GGSharpPool
{
    public abstract class PoolBase : IPool
    {
        #region Properties

        public int capacityMin
        {
            get => _capacityMin;
            set => SetCapacityMin(value);
        }

        public int capacityMax
        {
            get => _capacityMax;
            set => SetCapacityMax(value);
        }

        public int spilloverAllowance { get; set; }

        public int instanceCount => _pool.Count;
        
        public int activeCount => GetPoolActiveCount();
        
        public int recyclesCount { get; private set; }

        public int activeSpilloverCount => _capacityMax > 0 ? Math.Max(0, _pool.Count - _capacityMax) : 0;

        public int pooledUseCount => recyclesCount + _availableUsedCount;
        
        public string poolLabel { get; set; }

        #endregion Properties


        #region Fields

        /// <summary>
        /// The pool of instances, ordered from oldest (0) to newest (count - 1)
        /// </summary>
        private readonly List<IClientPoolable> _pool = new List<IClientPoolable>();

        // Backing fields for properties
        private int _capacityMin;
        private int _capacityMax = -1;
        
        /// <summary>
        /// Counts the number of times pooled instances (already instantiated) are used
        /// </summary>
        private int _availableUsedCount;

        #endregion Fields


        #region Constructor

        /// <summary>
        /// Creates a pool with optional preconfigured data.
        /// </summary>
        protected PoolBase(PoolConfigData configData = null)
        {
            if (configData != null)
            {
                capacityMin = configData.minimumCapacity;
                capacityMax = configData.maximumCapacity;
                spilloverAllowance = configData.spilloverAllowance;
                poolLabel = configData.label;
            }
        }

        #endregion Constructor


        #region Get Next
        
        public IClientPoolable GetNext()
        {
            // Get oldest pool object
            IClientPoolable result;
            if (_pool.Count > 0)
            {
                result = GetNextAvailable();
                if (result == null)
                {
                    // We are either full or at max capacity. Create or recycle/spillover
                    if (_capacityMax > 0 && _pool.Count >= _capacityMax)
                    {
                        if (spilloverAllowance == -1 || 
                            (spilloverAllowance > 0 && _pool.Count < _capacityMax + spilloverAllowance))
                        {
                            result = Spillover();
                        }
                        else
                        {
                            result = Recycle();
                        }
                    }
                    else
                    {
                        result = CreateNew(true);
                    }
                }
                else
                {
                    _availableUsedCount++;
                    ClaimInstance(result, false);
                }
            }
            else
            {
                result = CreateNew(true);
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private IClientPoolable GetNextAvailable()
        {
            return _pool[0].availableInPool ? _pool[0] : null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private IClientPoolable CreateNew(bool activateAfterCreation)
        {
            IClientPoolable result = CreateNewPoolable();
            result.OnInstanceCreated(this);

            // Claim (if using) or relinquish (if prewarming)
            if (activateAfterCreation)
            {
                ClaimInstance(result, true);
            }
            else
            {
                RelinquishInstance(result);
            }
            
            return result;
        }
        
        protected abstract IClientPoolable CreateNewPoolable();
        
        #endregion Get Next


        #region Overflow

        /// <summary>
        /// Recycles and claims the oldest instance of the pooled objects.
        /// </summary>
        /// <returns></returns>
        private IClientPoolable Recycle()
        {
            // Find oldest, relinquish, re-activate
            IClientPoolable p = _pool[0];
            _pool.RemoveAt(0);
            _pool.Add(p);
            p.Recycle();
            
            recyclesCount++;
            return p;
        }
        
        /// <summary>
        /// Creates a new instance, but marks it for deletion.
        /// </summary>
        /// <returns></returns>
        private IClientPoolable Spillover()
        {
            IClientPoolable p = CreateNew(true);
            return p;
        }

        #endregion Overflow


        #region Ownership

        public void ClaimInstance(IClientPoolable instance, bool isNewInstance)
        {
            if (!isNewInstance)
            {
                _pool.Remove(instance);
            }

            _pool.Add(instance);
            instance.availableInPool = false;
            instance.Claim();
        }

        public void RelinquishInstance(IClientPoolable instance)
        {
            // If we are in spillover, the instance should be deleted
            if (_capacityMax > 0 && _pool.Count > _capacityMax)
            {
                instance.DeleteFromPool();
                _pool.Remove(instance);
            }
            // Otherwise relenquish as normal
            else
            {
                _pool.Remove(instance);
                _pool.Insert(0, instance);
                instance.availableInPool = true;
                instance.Relinquish();
            }
        }
        
        void IPool.DeleteFromInstance(IClientPoolable instance)
        {
            _pool.Remove(instance);
        }

        #endregion Ownership


        #region Capactiy

        /// <summary>
        /// 
        /// </summary>
        /// <param name="minValue"></param>
        private void SetCapacityMin(int minValue)
        {
            if (minValue == _capacityMin)
                return;
            
            _capacityMin = minValue;
            if (!PoolValidationUtility.ValidatePoolCapacity(_capacityMin, _capacityMax))
                return;
            
            // We need to create objects if we don't have enough
            EnforceMinimumCapacity();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxValue"></param>
        private void SetCapacityMax(int maxValue)
        {
            if (maxValue == _capacityMax)
                return;
            
            _capacityMax = maxValue;
            if (!PoolValidationUtility.ValidatePoolCapacity(_capacityMin, _capacityMax))
                return;
            
            EnforceMaximumCapacity();
        }

        /// <summary>
        /// Ensures that there are a minimum number of instaces in the pool
        /// </summary>
        private void EnforceMinimumCapacity()
        {
            int createCount = _capacityMin - _pool.Count;
            for (int i = 0; i < createCount; i++)
            {
                CreateNew(false);
            }
        }

        /// <summary>
        /// Ensures that instances are destroyed if the instance count exceeds the pool max capacity
        /// </summary>
        private void EnforceMaximumCapacity()
        {
            if (_capacityMax < 0)
                return;
            
            int removalCount = _pool.Count - _capacityMax;
            if (removalCount > 0)
            {
                // Remove oldest objects first
                for (int i = 0; i < _pool.Count; i++)
                {
                    if (removalCount <= 0)
                        break;
                
                    IClientPoolable p = _pool[i];
                    p.DeleteFromPool();
                    _pool.Remove(p);
                    removalCount--;
                }
            }
        }

        #endregion Capacity


        #region Utility

        void IPool.Clean()
        {
            // Determine number of instances to clean
            int cleanCount = 0;
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i].availableInPool)
                {
                    cleanCount++;
                }
                else
                {
                    break;
                }
            }
            
            // Clean instances
            int cleaned = 0;
            while (cleaned < cleanCount)
            {
                _pool[0].DeleteFromPool();
                _pool.RemoveAt(0);
                cleaned++;
            }
        }
        
        void IPool.Clear()
        {
            for (int i = _pool.Count - 1; i >= 0; i--)
            {
                IClientPoolable p = _pool[i];
                p.DeleteFromPool();
                _pool.Remove(p);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private int GetPoolActiveCount()
        {
            int a = 0;
            for (int i = _pool.Count - 1; i >= 0; i--)
            {
                if (!_pool[i].availableInPool)
                {
                    a++;
                }                
                else
                {
                    break;
                }
            }

            return a;
        }

        #endregion Utility
    }
}