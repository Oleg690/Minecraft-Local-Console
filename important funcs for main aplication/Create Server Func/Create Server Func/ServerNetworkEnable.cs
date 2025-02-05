using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace NetworkConfig
{
    class NetworkConfigSetup
    {
        public static async Task Setup(int port)
        {
            if (!IsAdministrator())
            {
                Console.WriteLine("Restarting as Administrator...");
                RestartAsAdmin();
                return;
            }

            Console.WriteLine("Starting Network Configuration...");

            // Step 1: Port Forwarding using UPnP
            // NetworkTunnel -> TODO

            // Step 2: Set Static IP
            StaticIPConfig.SetStaticIP("192.168.1.100", "255.255.255.0", "192.168.1.1", "8.8.8.8", "8.8.4.4");

            // Step 3: Open Firewall Port
            FirewallRules.OpenFirewallPort(port);

            Console.WriteLine("Network setup completed!");
        }

        private static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
        private static void RestartAsAdmin()
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = Process.GetCurrentProcess().MainModule.FileName,
                Verb = "runas",
                UseShellExecute = true
            };
            try
            {
                Process.Start(psi);
                Environment.Exit(0);
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to start as Administrator. Please run manually as Admin.");
                Environment.Exit(1);
            }
        }
    }

    public class NetworkTunnel
    {

    }

    public class StaticIPConfig
    {
        public static void SetStaticIP(string localIP, string subnetMask, string gateway, string dns1, string dns2)
        {
            string adapterName = GetActiveAdapterName();

            // Check current IP configuration
            string currentIP = GetCurrentIP(adapterName);
            string currentSubnetMask = GetCurrentSubnetMask(adapterName);
            string currentGateway = GetCurrentGateway(adapterName);
            string currentDNS1 = GetCurrentDNS(adapterName, 1);
            string currentDNS2 = GetCurrentDNS(adapterName, 2);

            if (currentIP != localIP || currentSubnetMask != subnetMask || currentGateway != gateway)
            {
                string ipCommand = $"netsh interface ip set address name=\"{adapterName}\" static {localIP} {subnetMask} {gateway}";
                Execute_Command.Execute(ipCommand);
            }

            if (currentDNS1 != dns1)
            {
                string dnsCommand = $"netsh interface ip set dns name=\"{adapterName}\" static {dns1} primary";
                Execute_Command.Execute(dnsCommand);
            }

            if (currentDNS2 != dns2)
            {
                string dnsAltCommand = $"netsh interface ip add dns name=\"{adapterName}\" {dns2} index=2";
                Execute_Command.Execute(dnsAltCommand);
            }

            Console.WriteLine("Static IP configuration verified and updated if necessary.");
        }

        private static string GetActiveAdapterName()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up &&
                    (nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                     nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
                {
                    return nic.Name;
                }
            }
            return "Ethernet"; // Default fallback
        }

        private static string GetCurrentIP(string adapterName)
        {
            string command = $"netsh interface ip show config name=\"{adapterName}\"";
            string output = Execute_Command.ExecuteWithOutput(command);
            return ExtractValue(output, "IP Address");
        }

        private static string GetCurrentSubnetMask(string adapterName)
        {
            string command = $"netsh interface ip show config name=\"{adapterName}\"";
            string output = Execute_Command.ExecuteWithOutput(command);
            return ExtractValue(output, "Subnet Prefix");
        }

        private static string GetCurrentGateway(string adapterName)
        {
            string command = $"netsh interface ip show config name=\"{adapterName}\"";
            string output = Execute_Command.ExecuteWithOutput(command);
            return ExtractValue(output, "Default Gateway");
        }

        private static string GetCurrentDNS(string adapterName, int index)
        {
            string command = $"netsh interface ip show dns name=\"{adapterName}\"";
            string output = Execute_Command.ExecuteWithOutput(command);
            return ExtractValue(output, $"DNS Servers[{index}]");
        }

        private static string ExtractValue(string output, string key)
        {
            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                if (line.Contains(key))
                {
                    return line.Split(new[] { ':' }, 2)[1].Trim();
                }
            }
            return string.Empty;
        }
    }

    public class FirewallRules
    {
        public static void OpenFirewallPort(int port)
        {
            if (!FirewallRuleExists($"MinecraftServer_TCP_{port}"))
            {
                AddFirewallRule($"MinecraftServer_TCP_{port}", port, "TCP");
            }

            if (!FirewallRuleExists($"MinecraftServer_UDP_{port}"))
            {
                AddFirewallRule($"MinecraftServer_UDP_{port}", port, "UDP");
            }

            Console.WriteLine($"Firewall rules verified and updated if necessary for port {port}.");
        }

        private static void AddFirewallRule(string ruleName, int port, string protocol)
        {
            string command = $"netsh advfirewall firewall add rule name=\"{ruleName}\" dir=in action=allow protocol={protocol} localport={port}";
            Execute_Command.Execute(command);
        }

        private static bool FirewallRuleExists(string ruleName)
        {
            string command = $"netsh advfirewall firewall show rule name=\"{ruleName}\"";
            string output = Execute_Command.ExecuteWithOutput(command);
            return output.Contains("Rule Name");
        }
    }

    class Execute_Command
    {
        public static void Execute(string command)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C {command}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            Console.WriteLine(result);
            process.WaitForExit();
        }

        public static string ExecuteWithOutput(string command)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C {command}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }
    }
}
