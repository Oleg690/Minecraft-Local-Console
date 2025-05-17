using CreateServerFunc;
using System.IO;
using System.Windows;

namespace Minecraft_Console.ServerControl;
public class ServerManager(ServerInfoViewModel viewModel)
{
    private readonly ServerInfoViewModel _viewModel = viewModel;

    public async Task<bool> StartServer(string worldNumber, string rootWorldsFolder, string publicIP, Func<bool> isServerRunning, Action setServerRunningTrue, string serverDirectoryPath, Action<string>? onServerRunning = null)
    {
        if (isServerRunning())
        {
            MessageBox.Show("A server is already running.");
            return false;
        }

        if (!ValidateFields(rootWorldsFolder, worldNumber, publicIP))
            return false;

        var fullPath = Path.Combine(rootWorldsFolder, worldNumber);

        if (!TryGetServerPorts(worldNumber, out int serverPort, out int jmxPort, out int rconPort, out int rmiPort))
        {
            MessageBox.Show("Server port configuration not found.");
            return false;
        }

        await Task.Run(() => ServerOperator.Start(
            worldNumber,
            fullPath,
            5000,
            publicIP,
            serverPort,
            jmxPort,
            rconPort,
            rmiPort,
            noGUI: false,
            viewModel: _viewModel,
            onServerRunning: onServerRunning
        ));

        // Wait until serverRunning becomes true (set externally)
        while (!isServerRunning())
            await Task.Delay(500);

        setServerRunningTrue?.Invoke();
        return true;
    }

    public static async Task<bool> StopServer(string worldNumber, string localIP, Action setServerRunningFalse)
    {
        if (!ValidateFields(worldNumber, localIP))
            return false;

        if (!TryGetRCONPort(worldNumber, out int rconPort))
        {
            MessageBox.Show("No RCON port found.");
            return false;
        }

        setServerRunningFalse?.Invoke();

        await Task.Run(() => ServerOperator.Stop("stop", worldNumber, localIP, rconPort, "00:00"));

        return true;
    }

    public async Task<bool> RestartServer(string worldNumber, string rootWorldsFolder, string localIP, string publicIP, Func<bool> isServerRunning, Action<string>? onServerRunning = null)
    {
        if (!isServerRunning())
        {
            MessageBox.Show("No server is running.");
            return false;
        }

        if (!ValidateFields(worldNumber, rootWorldsFolder, localIP, publicIP))
            return false;

        var fullPath = Path.Combine(rootWorldsFolder, worldNumber);

        if (!TryGetServerPorts(worldNumber, out int serverPort, out int jmxPort, out int rconPort, out int rmiPort))
        {
            MessageBox.Show("No server configuration found.");
            return false;
        }

        await Task.Run(() => ServerOperator.Stop("stop", worldNumber, localIP, rconPort, "00:00"));

        await Task.Run(() => ServerOperator.Start(
            worldNumber,
            fullPath,
            5000,
            publicIP,
            serverPort,
            jmxPort,
            rconPort,
            rmiPort,
            noGUI: false,
            viewModel: _viewModel,
            onServerRunning: onServerRunning
        ));

        while (!isServerRunning())
            await Task.Delay(500);

        return true;
    }

    private static bool ValidateFields(params string[] fields)
    {
        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field))
            {
                MessageBox.Show("One or more required fields are not set.");
                return false;
            }
        }
        return true;
    }

    private static bool TryGetServerPorts(string worldNumber, out int serverPort, out int jmxPort, out int rconPort, out int rmiPort)
    {
        serverPort = jmxPort = rconPort = rmiPort = 0;

        var data = dbChanger.SpecificDataFunc(
            $"SELECT Server_Port, JMX_Port, RCON_Port, RMI_Port FROM worlds WHERE worldNumber = \"{worldNumber}\";"
        );

        if (data.Count == 0) return false;

        var row = data[0];
        serverPort = Convert.ToInt32(row[0]);
        jmxPort = Convert.ToInt32(row[1]);
        rconPort = Convert.ToInt32(row[2]);
        rmiPort = Convert.ToInt32(row[3]);

        return true;
    }

    private static bool TryGetRCONPort(string worldNumber, out int rconPort)
    {
        rconPort = 0;

        var data = dbChanger.SpecificDataFunc(
            $"SELECT RCON_Port FROM worlds WHERE worldNumber = \"{worldNumber}\";"
        );

        if (data.Count == 0) return false;

        rconPort = Convert.ToInt32(data[0][0]);
        return true;
    }
}
