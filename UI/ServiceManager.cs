using System.ServiceProcess;

namespace LoginGuardUI;

public static class ServiceManager
{
    public const string ServiceName = "LoginGuardService";
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    public static ServiceControllerStatus? GetStatus()
    {
        try
        {
            using var sc = new ServiceController(ServiceName);
            return sc.Status;
        }
        catch (InvalidOperationException)
        {
            // Service not installed
            return null;
        }
        catch
        {
            return null;
        }
    }

    public static bool IsRunning()
    {
        return GetStatus() == ServiceControllerStatus.Running;
    }

    public static void Start(TimeSpan? timeout = null)
    {
        using var sc = new ServiceController(ServiceName);
        if (sc.Status != ServiceControllerStatus.Running && sc.Status != ServiceControllerStatus.StartPending)
        {
            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running, timeout ?? DefaultTimeout);
        }
    }

    public static void Stop(TimeSpan? timeout = null)
    {
        using var sc = new ServiceController(ServiceName);
        if (sc.Status != ServiceControllerStatus.Stopped && sc.Status != ServiceControllerStatus.StopPending)
        {
            sc.Stop();
            sc.WaitForStatus(ServiceControllerStatus.Stopped, timeout ?? DefaultTimeout);
        }
    }

    public static void Restart(TimeSpan? timeout = null)
    {
        TimeSpan opTimeout = timeout ?? DefaultTimeout;
        using var sc = new ServiceController(ServiceName);

        if (sc.Status != ServiceControllerStatus.Stopped)
        {
            sc.Stop();
            sc.WaitForStatus(ServiceControllerStatus.Stopped, opTimeout);
        }

        sc.Start();
        sc.WaitForStatus(ServiceControllerStatus.Running, opTimeout);
    }
}
