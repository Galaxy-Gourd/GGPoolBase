using System;
using System.Collections.Generic;
using GG.Data.Base;
using UnityEngine;

namespace GGPoolBase
{
    public abstract class Pool : IPool
    {
        #region VARIABLES

        // Properties
        public int CapacityMin
        {
            get => _capacityMin;
            set => SetCapacityMin(value);
        }
        public int CapacityMax
        {
            get => _capacityMax;
            set => SetCapacityMax(value);
        }
        public int SpilloverAllowance { get; set; }
        public string PoolLabel { get; set; }
        public ITelemetry<DataTelemetryPool> Telemetry { get; }

        // Telemetry
        internal int InstanceCount => _pool.Count;
        internal int ActiveCount => GetPoolActiveCount();
        internal int RecyclesCount;
        internal int ActiveSpilloverCount => _capacityMax > 0 ? Math.Max(0, _pool.Count - _capacityMax) : 0;
        internal int PooledUseCount => RecyclesCount + _availableUsedCount;
        
        
        // Private
            
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

        #endregion VARIABLES


        #region CONSTRUCTOR

        /// <summary>
        /// Creates a pool with optional preconfigured data.
        /// </summary>
        protected Pool(DataConfigPool configData = null)
        {
            if (configData != null)
            {
                CapacityMin = configData.MinimumCapacity;
                CapacityMax = configData.MaximumCapacity;
                SpilloverAllowance = configData.SpilloverAllowance;
                PoolLabel = configData.Label;
            }
            
            // Create telemetry module
            Telemetry = new TelemetryPool();
        }

        #endregion CONSTRUCTOR


        #region RETRIEVAL

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
                        if (SpilloverAllowance == -1 || 
                            (SpilloverAllowance > 0 && _pool.Count < _capacityMax + SpilloverAllowance))
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

            UpdateTelemetry();
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private IClientPoolable GetNextAvailable()
        {
            return _pool[0].AvailableInPool ? _pool[0] : null;
        }

        /// <summary>
        /// Instantiates a brand-new instance of the poolable.
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
        
        /// <summary>
        /// Creates and returns a new instance of the IClientPoolable and adds it to the pool.
        /// </summary>
        /// <returns></returns>
        protected abstract IClientPoolable CreateNewPoolable();
        
        #endregion RETRIEVAL


        #region OVERFLOW

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
            
            RecyclesCount++;
            return p;
        }
        
        /// <summary>
        /// Creates a new instance, but marks it for deletion.
        /// </summary>
        private IClientPoolable Spillover()
        {
            IClientPoolable p = CreateNew(true);
            return p;
        }

        #endregion OVERFLOW


        #region OWNERSHIP

        public void ClaimInstance(IClientPoolable instance, bool isNewInstance)
        {
            if (!isNewInstance)
            {
                _pool.Remove(instance);
            }

            _pool.Add(instance);
            instance.AvailableInPool = false;
            instance.Claim();
            
            UpdateTelemetry();
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
                instance.AvailableInPool = true;
                instance.Relinquish();
            }
            
            UpdateTelemetry();
        }
        
        void IPool.DeleteFromInstance(IClientPoolable instance)
        {
            _pool.Remove(instance);
            UpdateTelemetry();
        }

        #endregion OWNERSHIP


        #region CAPACITY

        private void SetCapacityMin(int minValue)
        {
            if (minValue == _capacityMin)
                return;
            
            _capacityMin = minValue;
            if (!PoolValidationUtility.ValidatePoolCapacity(_capacityMin, _capacityMax))
                return;
            
            // We need to create objects if we don't have enough
            EnforceMinimumCapacity();
            UpdateTelemetry();
        }

        private void SetCapacityMax(int maxValue)
        {
            if (maxValue == _capacityMax)
                return;
            
            _capacityMax = maxValue;
            if (!PoolValidationUtility.ValidatePoolCapacity(_capacityMin, _capacityMax))
                return;
            
            EnforceMaximumCapacity(_pool, _capacityMax);
            UpdateTelemetry();
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
        private static void EnforceMaximumCapacity(
            List<IClientPoolable> pool,
            int maxCapacity)
        {
            if (maxCapacity < 0)
                return;
            
            int removalCount = pool.Count - maxCapacity;
            if (removalCount > 0)
            {
                // Remove oldest objects first; gather in separate list to prevent list iteration issues
                List<IClientPoolable> toRemove = new List<IClientPoolable>();
                for (int i = 0; i < pool.Count; i++)
                {
                    if (removalCount <= 0)
                        break;
                
                    toRemove.Add(pool[i]);
                    removalCount--;
                }
                
                foreach (IClientPoolable client in toRemove)
                {
                    client.DeleteFromPool();
                    pool.Remove(client);
                }
            }
        }

        #endregion CAPACITY


        #region UTILITY

        void IPool.Clean()
        {
            // Determine number of instances to clean
            int cleanCount = 0;
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i].AvailableInPool)
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
            
            UpdateTelemetry();
        }
        
        void IPool.Clear()
        {
            for (int i = _pool.Count - 1; i >= 0; i--)
            {
                IClientPoolable p = _pool[i];
                p.DeleteFromPool();
                _pool.Remove(p);
            }
            
            UpdateTelemetry();
        }

        private int GetPoolActiveCount()
        {
            int a = 0;
            for (int i = _pool.Count - 1; i >= 0; i--)
            {
                if (!_pool[i].AvailableInPool)
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

        private void UpdateTelemetry()
        {
            // Update telemetry
            (Telemetry as TelemetryPool).Broadcast(this);
        }

        #endregion UTILITY
    }
}