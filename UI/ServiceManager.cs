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
        TimeSpan opTimeout = timeout ?? DefaultTimeout;
        using var sc = new ServiceController(ServiceName);
        sc.Refresh();

        if (sc.Status == ServiceControllerStatus.StartPending)
        {
            sc.WaitForStatus(ServiceControllerStatus.Running, opTimeout);
        }
        else if (sc.Status != ServiceControllerStatus.Running)
        {
            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running, opTimeout);
        }
    }

    public static void Stop(TimeSpan? timeout = null)
    {
        TimeSpan opTimeout = timeout ?? DefaultTimeout;
        using var sc = new ServiceController(ServiceName);
        sc.Refresh();

        if (sc.Status == ServiceControllerStatus.StopPending)
        {
            sc.WaitForStatus(ServiceControllerStatus.Stopped, opTimeout);
        }
        else if (sc.Status != ServiceControllerStatus.Stopped)
        {
            sc.Stop();
            sc.WaitForStatus(ServiceControllerStatus.Stopped, opTimeout);
        }
    }

    public static void Restart(TimeSpan? timeout = null)
    {
        TimeSpan opTimeout = timeout ?? DefaultTimeout;
        using var sc = new ServiceController(ServiceName);
        sc.Refresh();

        if (sc.Status == ServiceControllerStatus.StopPending)
        {
            sc.WaitForStatus(ServiceControllerStatus.Stopped, opTimeout);
            sc.Refresh();
        }
        else if (sc.Status != ServiceControllerStatus.Stopped)
        {
            sc.Stop();
            sc.WaitForStatus(ServiceControllerStatus.Stopped, opTimeout);
            sc.Refresh();
        }

        if (sc.Status == ServiceControllerStatus.StartPending)
        {
            sc.WaitForStatus(ServiceControllerStatus.Running, opTimeout);
        }
        else if (sc.Status != ServiceControllerStatus.Running)
        {
            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running, opTimeout);
        }
    }
}
