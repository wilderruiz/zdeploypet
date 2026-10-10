namespace ZDeployPet.Core;

public sealed class DeploymentExecutionGate
{
    private int _running;

    public bool IsRunning => Volatile.Read(ref _running) != 0;

    public bool TryAcquire(out IDisposable? lease)
    {
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        {
            lease = null;
            return false;
        }

        lease = new Lease(this);
        return true;
    }

    private void Release() => Interlocked.Exchange(ref _running, 0);

    private sealed class Lease : IDisposable
    {
        private DeploymentExecutionGate? _owner;

        public Lease(DeploymentExecutionGate owner) => _owner = owner;

        public void Dispose()
        {
            DeploymentExecutionGate? owner = Interlocked.Exchange(ref _owner, null);
            owner?.Release();
        }
    }
}
