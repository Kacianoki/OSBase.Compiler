using System.IO.Compression;
using System.Reflection;
using System.Diagnostics;
namespace OSBase.Compiler;
public class Compiler
{
    public static void Main(string[] args)
    {
        if (!Directory.Exists(@"tobuild\Release\net6.0"))
        {
            Console.WriteLine("Nothing to compile");
            Environment.Exit(-1);
        }
        List<OS> os = new List<OS>();
        string OSFile = string.Empty;
        foreach (var s in Directory.GetFiles(@"tobuild\Release\net6.0", "*.dll"))
        {
            if (new FileInfo(s).Name.StartsWith("OSBase.")) continue;
            var file = new FileInfo(s);
            Console.WriteLine(file.FullName);
            Assembly assembly = Assembly.LoadFrom(file.FullName);
            Type[] types = assembly.GetTypes();
            if(types.Length == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Unknown Error.");
                Console.ResetColor();
                continue;
            }
            List<Type> ConsoleOS = (from p in types where p.IsSubclassOf(typeof(ConsoleOS)) select p).ToList();
            List<Type> InstallerOS = (from p in types where p.IsSubclassOf(typeof(OSInstaller)) select p).ToList();
            if(ConsoleOS.Count > 1)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("More than 1 OS is detected in one build, this is not allowed. If you want to make more than one OS - create a new project.");
                Console.ResetColor();
                Console.WriteLine("Found classes \"ConsoleOS\":");
                foreach (var ins in ConsoleOS)
                {
                    Console.WriteLine(ins.FullName);
                }
                Console.Beep();
                continue;
                Console.ReadKey();
                Environment.Exit(-2);
            }
            if (ConsoleOS.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("OS class not detected.");
                Console.ResetColor();
                Console.Beep();
                continue;
                Console.ReadKey();
                Environment.Exit(-5);
            }
            if (InstallerOS.Count > 1)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("More than 1 \"OSInstaller\" is detected in one build, this is not allowed. If you want to create another OS, create a new project.");
                Console.ResetColor();
                Console.WriteLine("Found classes \"OSOSInstaller\":");
                foreach(var ins in InstallerOS)
                {
                    Console.WriteLine(ins.FullName);
                }
                Console.Beep();
                continue;
                Console.ReadKey();
                Environment.Exit(-2);
            }
            if (InstallerOS.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("There must be 1 class \"OSInstaller\" in the OS assembly. ");
                Console.Beep();
                continue;
                Console.ReadKey();
                Environment.Exit(-2);
            }
            int i = 0;
            foreach (var type in ConsoleOS)
            {
                try
                {
                    ConsoleOS consoleOSInstance = (ConsoleOS)Activator.CreateInstance(type);
                    OSInstaller OSInstallerInstance = (OSInstaller)Activator.CreateInstance(InstallerOS[i]);
                    Console.WriteLine($"Console OS found:\r\nOS Name:{consoleOSInstance.osinfo.Name}, OS Type:{consoleOSInstance.osinfo.Type}");
                    os.Add(new OS(consoleOSInstance, assembly, OSInstallerInstance));
                    OSFile = s;
                }
                catch
                {

                }
                i++;
            }
            
        }
        if (os.Count > 1)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("More than 1 OS is detected in one build, this is not allowed! If you want to make more than one OS - create a new project.");
            Console.Beep();
            Console.ReadKey();
            Environment.Exit(-2);
        }
        if (os.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Nothing to compile");
            Console.Beep();
            Console.ReadKey();
            Environment.Exit(-1);
        }
        int tasks = 19;
        int task = 0;
        void TaskEnded()
        {
            task++;
            Console.WriteLine(Math.Round((decimal)(task / tasks * 100)) + "%");
        }
        Console.WriteLine("Compilation...");
        var CompiledOS = new CompiledOS();
        CompiledOS.ConsoleOS = os.First().consoleOS.osinfo;
        TaskEnded();
        Type[] typess = os.First().Assembly.GetTypes();
        List<Type> ConsoleOSs = (from p in typess where p.IsSubclassOf(typeof(ConsoleOS)) select p).ToList();
        var methodInfo = ConsoleOSs.First().GetMethod("Main", BindingFlags.Static | BindingFlags.Public);
        if(methodInfo == null || methodInfo.ReturnType != typeof(void) || methodInfo.GetParameters().Length > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("The OS must have a static, returning void, with access modifier public and accepting no parameters method called \"Main\".");
            Console.Beep();
            Console.ReadKey();
            Environment.Exit(-4);
        }        
        TaskEnded();        
        var ReferencedAssemblies = os.First().Assembly.GetReferencedAssemblies();
        CompiledOS.DependentAssemblies = (from a in ReferencedAssemblies select new AssemblyInfo(a.Name, a.FullName, a.Version)).ToArray();
        TaskEnded();
        List<string> AssemblyFiles = new List<string>();
        foreach (var s in Directory.GetFiles(@"tobuild\Release\net6.0", "*.dll"))
        {
            Assembly asm = Assembly.LoadFrom(s);
            foreach(var ss in ReferencedAssemblies)
            {
                if (asm.GetName().FullName == ss.FullName)
                {
                    AssemblyFiles.Add(s);
                    break;
                }
            }
        }
        CompiledOS.DependentFiles = AssemblyFiles.ToArray();
        TaskEnded();
        CompiledOS.AssemblyFile = OSFile;
        TaskEnded();
        Directory.CreateDirectory(@"Temp\OS");
        TaskEnded();
        var fileinfo = new FileInfo(CompiledOS.AssemblyFile);
        TaskEnded();
        fileinfo.CopyTo(Path.Combine("Temp\\OS", fileinfo.Name), true);
        TaskEnded();
        foreach(var s in CompiledOS.DependentFiles)
        {
            var file = new FileInfo(s);
            file.CopyTo(Path.Combine("Temp\\OS", file.Name), true);
        }
        TaskEnded();
        List<string> CustomFiles = new List<string>();
        if (File.Exists("_AddToOs.txt"))
        {
            foreach(var s in File.ReadLines("_AddToOs.txt"))
            {
                if (s.StartsWith("//")) continue;
                try
                {
                    Console.WriteLine("Temp" + "\\OS" + "\\" + s.Replace(Directory.GetCurrentDirectory(), ""));
                    Directory.CreateDirectory("Temp" + "\\OS" + "\\" + s.Replace(Directory.GetCurrentDirectory(), "").Replace(new FileInfo(s).Name, ""));
                    Console.WriteLine("Temp" + "\\OS" + "\\" + s.Replace(Directory.GetCurrentDirectory(), ""));
                    File.Copy(s, "Temp" + "\\OS" + "\\" + s.Replace(Directory.GetCurrentDirectory(), "") + new FileInfo(s).Name);
                    CustomFiles.Add(Path.Combine(s.Replace(Directory.GetCurrentDirectory(), ""), new FileInfo(s).Name));
                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Failed to add to the build \"{s}\"");
                    Console.Beep();
                    Console.ResetColor();
                    Console.ReadKey();
                }
            }

        }
        TaskEnded();
        CompiledOS.CompilerVersion = Environment.Version;
        TaskEnded();
        CompiledOS.AssemblyFile = new FileInfo(OSFile).Name;
        TaskEnded();
        List<string> AssemblyFiless = new List<string>();
        foreach (var s in AssemblyFiles)
        {
            AssemblyFiless.Add(new FileInfo(s).Name);
        }
        TaskEnded();
        CustomFiles.AddRange(AssemblyFiless);
        CompiledOS.DependentFiles = CustomFiles.ToArray();
        TaskEnded();
        File.WriteAllText(Path.Combine("Temp", "OS.json"), Newtonsoft.Json.JsonConvert.SerializeObject(CompiledOS));
        TaskEnded();
        Directory.CreateDirectory("Output");
        TaskEnded();
        if (File.Exists(@$"Output\{CompiledOS.ConsoleOS.Name}_{CompiledOS.ConsoleOS.Type}.zip")) File.Delete(@$"Output\{CompiledOS.ConsoleOS.Name}_{CompiledOS.ConsoleOS.Type}.zip");
        TaskEnded();
        ZipFile.CreateFromDirectory("Temp", @$"Output\{CompiledOS.ConsoleOS.Name}_{CompiledOS.ConsoleOS.Type}.zip", CompressionLevel.SmallestSize, false);
        TaskEnded();
        Directory.Delete("Temp", true);
        TaskEnded();
        ProcessStartInfo startInfo = new ProcessStartInfo(new FileInfo(@$"Output\{CompiledOS.ConsoleOS.Name}_{CompiledOS.ConsoleOS.Type}.zip").Directory.FullName);
        startInfo.UseShellExecute = true;
        Process.Start(startInfo);
        Environment.Exit(0);
    }
}
public class CompiledOS
{
    public ConsoleOSInfo ConsoleOS;
    public string AssemblyFile;
    public AssemblyInfo[] DependentAssemblies;
    public string[] DependentFiles;
    public Version CompilerVersion;
}
public class OS
{
    public ConsoleOS consoleOS;
    public OSInstaller installer;
    public Assembly Assembly;
    public OS(ConsoleOS consoleOS, Assembly assembly, OSInstaller installer)
    {
        this.consoleOS = consoleOS;
        Assembly = assembly;
        this.installer = installer;
    }
}
public class AssemblyInfo
{
    public string Name;
    public string FullName;
    public Version Version;
    public AssemblyInfo(string name, string fullName, Version version)
    {
        Name = name;
        FullName = fullName;
        Version = version;
    }
}