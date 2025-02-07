using Open.Nat;
using System;
using System.Diagnostics;
using System.Net;
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

            if (IsAdministrator())
            {

                return;
            }

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

    public class UPnP_Port_Mapping
    {
        public static async Task UPnP_Configuration_Async(int port)
        {
            try
            {
                // Discover a UPnP-enabled router
                var nat = new NatDiscoverer();
                var device = await nat.DiscoverDeviceAsync();

                // Get the local IP address of the machine
                string localIp = GetLocalIpAddress();
                Console.WriteLine(localIp);

                // Define the port mapping
                int externalPort = port;
                int internalPort = port;
                string description = "Minecraft Server Port Forwarding";

                // Add the port mapping for TCP
                await device.CreatePortMapAsync(new Mapping(Protocol.Tcp, internalPort, externalPort, description));

                Console.WriteLine($"Port mapping successful! External port {externalPort} is now forwarded to {localIp}:{internalPort}.");
            }
            catch (NatDeviceNotFoundException)
            {
                Console.WriteLine("Device not found. You probably don't have UPnP enabled in the router settings or the router doesn't support it! You need to set port mapping manually!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address found.");
        }
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

    class DomainName
    {
        private static readonly string[] Combinations = {
        "skilfish", "cleverfox", "swiftdeer", "bravebear", "quietmouse", "loyalwolf",
        "wildhorse", "tinyant", "buzzingbee", "soaringhawk", "playfuldolphin",
        "gentlelamb", "roaringlion", "brightstar", "goldenkey", "silvercoin",
        "woodenbox", "ironwheel", "glassbottle", "paperplane", "stonebridge",
        "metalgate", "velvetcurtain", "crystalball", "rainbowarch", "sweetberry",
        "juicyapple", "freshmint", "spicychili", "creamycoffee", "goldenhoney",
        "crispbread", "warmmilk", "coolwater", "ripeplum", "deepthought",
        "brightidea", "strongwill", "kindheart", "truefriend", "braveheart",
        "quickwit", "sharpfocus", "calmspirit", "wilddream", "newhope",
        "greenfield", "blueocean", "tallmountain", "deepforest", "clearriver",
        "sunnyvale", "quietcove", "hiddenpath", "secretgarden", "rockycoast",
        "codingape", "singingbird", "dancingbear", "readingworm", "writingowl",
        "thinkingcap", "buildingblock", "exploringfox", "dreamingdog", "learningbee",
        "happycat", "sleepybear", "grumpyfrog", "sillygoose", "cleverdog", "wiseowl",
        "darkknight", "loudlion", "swiftbluefish", "goldenkeybox", "sweetberryfield",
        "deepthoughtwell", "braveheartlion", "cuddlykoala", "mischievousmonkey",
        "sneakycat", "gracefulswan", "proudpeacock", "hungryhippo", "jollygiraffe",
        "lazyiguana", "quickrabbit", "slowsloth", "wiseelephant", "tinyhummingbird",
        "giantwhale", "fierydragon", "icequeen", "stormbringer", "shadowwalker",
        "sunriser", "moonwhisper", "stargazer", "dreamweaver", "lifeforce",
        "timebender", "spacejumper", "wordmaster", "artlover", "musicmaker",
        "codebreaker", "peacemaker", "storyteller", "adventurer", "explorer",
        "innovator", "creator", "leader", "follower", "teacher", "student",
        "friend", "lover", "hater", "winner", "loser", "beginner", "expert",
        "hero", "villain", "angel", "demon", "ghost", "zombie", "vampire",
        "werewolf", "mermaid", "unicorn", "phoenix", "griffin", "sphinx",
        "chimera", "hydra", "cerberus", "minotaur", "cyclops", "medusa",
        "poseidon", "hades", "zeus", "hera", "apollo", "artemis", "athena",
        "aphrodite", "ares", "hephaestus", "dionysus", "hermes", "demeter",
        "hestia", "eros", "psyche", "pan", "morpheus", "hypnos", "thanatos",
        "nemesis", "hecate", "tyche", "nike", "iris", "ganymede", "hebe",
        "asclepius", "hygieia", "panacea", "telesphorus", "machon", "podalirius",
        "achaon", "proteus", "triton", "nereus", "amphitrite", "thetis",
        "galatea", "polyphemus", "scylla", "charybdis", "siren", "harpy",
        "gorgon", "centaur", "satyr", "nymph", "dryad", "naiad", "nereid",
        "oceanid", "titan", "giant", "cyclops", "hecatoncheires", "argonaut",
        "heracleidae", "trojan", "odyssean", "aeneid", "iliad", "odyssey",
        "aeneid", "metamorphoses", "deorum", "fabulis", "astronomica",
        "geographica", "historia", "naturalis", "bibliotheca", "mythologica",
        "etymologiae", "chronicon", "paschale", "alexandrinum", "chronographia",
        "breviarium", "historiae", "augustae", "annales", "prudentii", "psychomachia",
        "somnium", "scipionis", "consolatio", "philosophiae", "de", "civitate",
        "de", "trinitate", "dialogi", "de", "libero", "arbitrio", "confessiones",
        "enchiridion", "de", "doctrina", "christiana", "de", "genesi", "ad", "litteram",
        "de", "trinitate", "contra", "faustum", "manichaeum", "de", "vera", "religione",
        "de", "immortalitate", "animae", "de", "quantitate", "animae", "musica",
        "disciplinae", "mathematicae", "de", "magistro", "de", "beata", "vita",
        "de", "ordine", "de", "vera", "religione", "contra", "academicos", "de",
        "divinatione", "de", "natura", "deorum", "de", "officiis", "de", "amicitia",
        "de", "senectute", "paradoxa", "stoicorum", "epistulae", "ad", "atticum",
        "ad", "familiares", "ad", "brutum", "ad", "quintum", "in", "catilinam",
        "pro", "caelio", "pro", "milone", "pro", "plancio", "pro", "rege", "deiotaro",
        "philippicae", "topica", "de", "inventione", "orator", "brutus", "laelius",
        "de", "oratore", "partitiones", "oratoriae", "de", "finibus", "bonorum",
        "et", "malorum", "tusculanae", "disputationes", "academica", "priora", "analytica",
        "posteriora", "topica", "de", "interpretatione", "de", "anima", "de", "categoriis",
        "physica", "metaphysica", "ethica", "nicomachea", "magna", "moralia", "eudemian",
        "politica", "poetica", "rhetorica", "de", "caelo", "de", "generatione", "et",
        "corruptione", "meteorologica", "historia", "animalium", "de", "partibus", "animalium",
        "de", "motu", "animalium", "de", "incessu", "animalium", "de", "generatione", "animalium",
        "de", "sensu", "et", "sensibilibus", "de", "memoria", "et", "reminiscentia", "de",
        "somno", "et", "vigilia", "de", "insomniis", "de", "divinatione", "per", "somnia",
        "de", "longitudine", "et", "brevitate", "vitae", "de", "iuventute", "et", "senectute",
        "de", "vita", "et", "morte", "de", "respiratione", "de", "spiritu", "de", "malo",
        "de", "vegetabilibus", "de", "lapidibus", "de", "plantis", "de", "mirabilibus",
        "de", "melodia", "de", "poetica", "de", "rhetorica", "ad", "alexandrum", "de",
        "mundo", "de", "virtutibus", "de", "vitiis", "oecumonica", "magna", "quaestio",
        "mechanica", "optica", "catoptrica", "musica", "problematum", "mechanicorum",
        "physiognomonica", "ethica", "eudemica", "politica", "oecumonica", "magna", "moralia"
    };

        public static string GetRandomDomainName()
        {
            string domain;
            do
            {
                string randomCombination = DomainName.Combinations[new Random().Next(DomainName.Combinations.Length)];
                domain = $"{randomCombination}.olehost.me.host";
            }
            while (IsMinecraftServerDomain(domain).Result);

            return domain;
        }

        private static async Task<bool> IsMinecraftServerDomain(string domain, int defaultPort = 25565)
        {
            try
            {
                Console.WriteLine($"Checking {domain} domain name.");

                // 1. Resolve the domain name to an IP address
                IPAddress[] addresses = await Dns.GetHostAddressesAsync(domain);

                if (addresses.Length == 0)
                {
                    Console.WriteLine($"Domain {domain} could not be resolved.");
                    return false; // Domain doesn't exist
                }

                // 2. Try to connect to the resolved IP address on the Minecraft default port
                foreach (IPAddress address in addresses)
                {
                    try
                    {
                        using (TcpClient client = new TcpClient())
                        {
                            // Use a timeout to prevent indefinite blocking
                            var connectTask = client.ConnectAsync(address, defaultPort);
                            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2)); // 2-second timeout

                            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                            if (completedTask == timeoutTask)
                            {
                                Console.WriteLine($"Connection to {domain} timed out.");
                                continue; // Try the next IP address
                            }

                            await connectTask; // Wait for the connection to complete (if it didn't time out)
                            Console.WriteLine($"Successfully connected to {domain} ({address}) on port {defaultPort}.");
                            return true; // Minecraft server is likely running
                        }
                    }
                    catch (Exception ex)
                    {
                        // Catch exceptions like connection refused, etc.
                        Console.WriteLine($"Error connecting to {domain} ({address}): {ex.Message}");
                        // Don't immediately return false; try other IP addresses first
                    }
                }

                return false; // Could not connect to any resolved IP address
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resolving or checking domain {domain}: {ex.Message}");
                return false;
            }
        }
    }
}
