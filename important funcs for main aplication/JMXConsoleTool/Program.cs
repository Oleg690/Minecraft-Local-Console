using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace JMXConsoleTool
{
    [SupportedOSPlatform("windows")]
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: JMXConsoleTool.exe <jmxremote.access path> <jmxremote.password path>");
                return 1;
            }

            string accessPath = args[0];
            string passwordPath = args[1];

            try
            {
                if (File.Exists(accessPath)) File.Delete(accessPath);
                if (File.Exists(passwordPath)) File.Delete(passwordPath);

                string? dir = Path.GetDirectoryName(passwordPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string currentUser = Environment.UserName;

                File.WriteAllText(accessPath, "");
                File.WriteAllText(passwordPath, "");


                Console.WriteLine("Files created successfully.");

                // Set permissions
                FileSecurity security = new();
                security.SetOwner(new NTAccount(currentUser));
                security.SetAccessRuleProtection(true, false);

                FileSystemAccessRule rule = new(
                    new NTAccount(currentUser),
                    FileSystemRights.Read | FileSystemRights.Write,
                    AccessControlType.Allow
                );

                security.AddAccessRule(rule);
                FileInfo fileInfo = new(passwordPath);
                fileInfo.SetAccessControl(security);

                Console.WriteLine("Permissions set successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 2;
            }
        }
    }
}
