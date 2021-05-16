using GGSharpData;

namespace GGSharpPool
{
    public class TelemetryPool : Telemetry<Pool, DataTelemetryPool>
    {
        #region FORMAT

        protected override void FormatData(Pool pool)
        {
            _data.InstanceCount = pool.InstanceCount;
            _data.ActiveCount = pool.ActiveCount;
            _data.RecyclesCount = pool.RecyclesCount;
            _data.ActiveSpilloverCount = pool.ActiveSpilloverCount;
            _data.PooledUseCount = pool.PooledUseCount;
        }

        #endregion FORMAT
    }
}