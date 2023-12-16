using OSBase;
using System.IO.Compression;
using System.Reflection;
using System.Diagnostics;
using System.IO.Enumeration;

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
            var file = new FileInfo(s);
            Console.WriteLine(file.FullName);
            Assembly assembly = Assembly.LoadFrom(file.FullName);
            Type[] types = assembly.GetTypes();
            List<Type> ConsoleOS = (from p in types where p.IsSubclassOf(typeof(ConsoleOS)) select p).ToList();
            foreach (var type in ConsoleOS)
            {
                try
                {
                    ConsoleOS consoleOSInstance = (ConsoleOS)Activator.CreateInstance(type);
                    Console.WriteLine($"Console OS found:\r\nOS Name:{consoleOSInstance.osinfo.Name}, OS Type:{consoleOSInstance.osinfo.Type}");
                    os.Add(new OS(consoleOSInstance, assembly));
                    OSFile = s;
                }
                catch
                {

                }

            }
            
        }
        if (os.Count > 1)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("More than 1 OS is detected in one build, this is not allowed! If you want to make more than one OS - create a new project.");
            Console.ReadKey();
            Environment.Exit(-2);
        }
        int tasks = 17;
        int task = 0;
        void TaskEnded()
        {
            task++;
            Console.WriteLine(Math.Round((decimal)(task / tasks * 100)) + "%");
        }
        Console.WriteLine("Compilation...");
        var CompiledOS = new CompiledOS();
        CompiledOS.ConsoleOS = os.First().consoleOS;
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
        CompiledOS.DependentFiles = AssemblyFiless.ToArray();
        TaskEnded();
        File.WriteAllText(Path.Combine("Temp", "OS.json"), Newtonsoft.Json.JsonConvert.SerializeObject(CompiledOS));
        TaskEnded();
        Directory.CreateDirectory("Output");
        TaskEnded();
        if (File.Exists(@$"Output\{CompiledOS.ConsoleOS.osinfo.Name}_{CompiledOS.ConsoleOS.osinfo.Type}.zip")) File.Delete(@$"Output\{CompiledOS.ConsoleOS.osinfo.Name}_{CompiledOS.ConsoleOS.osinfo.Type}.zip");
        TaskEnded();
        ZipFile.CreateFromDirectory("Temp", @$"Output\{CompiledOS.ConsoleOS.osinfo.Name}_{CompiledOS.ConsoleOS.osinfo.Type}.zip", CompressionLevel.SmallestSize, false);
        TaskEnded();
        Directory.Delete("Temp", true);
        TaskEnded();
        ProcessStartInfo startInfo = new ProcessStartInfo(new FileInfo(@$"Output\{CompiledOS.ConsoleOS.osinfo.Name}_{CompiledOS.ConsoleOS.osinfo.Type}.zip").Directory.FullName);
        startInfo.UseShellExecute = true;
        Process.Start(startInfo);
        Environment.Exit(0);
    }
}
public class CompiledOS
{
    public ConsoleOS ConsoleOS;
    public string AssemblyFile;
    public AssemblyInfo[] DependentAssemblies;
    public string[] DependentFiles;
    public Version CompilerVersion;
}
public class OS
{
    public ConsoleOS consoleOS;
    public Assembly Assembly;
    public OS(ConsoleOS consoleOS, Assembly assembly)
    {
        this.consoleOS = consoleOS;
        Assembly = assembly;
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