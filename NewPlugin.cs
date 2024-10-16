//reference System.dll
using MCGalaxy.Commands;
using MCGalaxy.Events.ServerEvents;
using MCGalaxy.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using MCGalaxy.New;
namespace MCGalaxy
{
    public class NewPluginLoader : Plugin
    {
        public override string creator { get { return "HarmonyNetwork"; } }
        public override string name { get { return "NewPluginLoader"; } }
        public static void LoadAllNewPlugins(SchedulerTask task)
        {
            NewPlugin.LoadAll();
        }
        public Command CmdNewPluginCreate = new CmdNewPluginCreate();
        public Command CmdNewPluginCompile = new CmdNewPluginCompile();
        public Command CmdNewPluginCompLoad = new CmdNewPluginCompLoad();
        public Command CmdPlugin1 = new CmdNewPlugin();
        public override void Load(bool startup)
        {
            Server.Critical.QueueOnce(LoadAllNewPlugins);
            NewPlugin.RegisterCmds(CmdNewPluginCreate,CmdNewPluginCompile, CmdNewPluginCompLoad, CmdPlugin1);
            OnShuttingDownEvent.Register(OnShutdown, Priority.Critical);
        }
        public void OnShutdown(bool restarting, string message)
        {
            Command.Unregister(CmdNewPluginCreate, CmdNewPluginCompile, CmdNewPluginCompLoad, CmdPlugin1);
            NewPlugin.UnloadAll();
        }
        public override void Unload(bool shutdown)
        {
            Command.Unregister(CmdNewPluginCreate, CmdNewPluginCompile, CmdNewPluginCompLoad, CmdPlugin1);
            NewPlugin.UnloadAll();
            OnShuttingDownEvent.Unregister(OnShutdown);
        }
        public override void Help(Player p)
        {
            p.Message("");
        }
    }
}
namespace MCGalaxy.New
{
    /// <summary> This class provides for more advanced modification to MCGalaxy </summary>
    public abstract class NewPlugin
    {
        public static void RegisterCmds(params Command[] commands)
        {
            foreach (Command cmd in commands) Command.Register(cmd);
        }
        /// <summary> Hooks into events and initalises states/resources etc </summary>
        /// <param name="auto"> True if the new plugin is being automatically loaded (e.g. on server startup), false if manually. </param>
        public abstract void Load(bool auto);

        /// <summary> Unhooks from events and disposes of state/resources etc </summary>
        /// <param name="auto"> True if the new plugin is being auto unloaded (e.g. on server shutdown), false if manually. </param>
        public abstract void Unload(bool auto);

        /// <summary> Called when a player does /Help on the new plugin. Typically tells the player what this new plugin is about. </summary>
        /// <param name="p"> Player who is doing /Help. </param>
        public virtual void Help(Player p)
        {
            p.Message("No help is available for this new plugin.");
        }

        /// <summary> Name of the new plugin. </summary>
        public abstract string name { get; }
        /// <summary> The oldest version of MCGalaxy this new plugin is compatible with. </summary>
        public virtual string MCGalaxy_Version { get { return null; } }
        /// <summary> Version of this new plugin. </summary>
        public virtual int build { get { return 0; } }
        /// <summary> Message to display once this new plugin is loaded. </summary>
        public virtual string welcome { get { return ""; } }
        /// <summary> The creator/author of this new plugin. (Your name) </summary>
        public virtual string creator { get { return ""; } }
        /// <summary> Whether or not to auto load this new plugin on server startup. </summary>
        public virtual bool LoadAtStartup { get { return true; } }


        /// <summary> List of new plugin/modules included in the server software </summary>
        public static List<NewPlugin> core = new List<NewPlugin>();
        public static List<NewPlugin> custom = new List<NewPlugin>();


        public static NewPlugin FindCustom(string name)
        {
            foreach (NewPlugin pl in custom)
            {
                if (pl.name.CaselessEq(name)) return pl;
            }
            return null;
        }

        public static void Load(NewPlugin pl, bool auto)
        {
            string ver = pl.MCGalaxy_Version;
            if (!string.IsNullOrEmpty(ver) && new Version(ver) > new Version(Server.Version))
            {
                string msg = string.Format("New plugin '{0}' requires a more recent version of {1}!", pl.name, Server.SoftwareName);
                throw new InvalidOperationException(msg);
            }
            try
            {
                custom.Add(pl);

                if (pl.LoadAtStartup || !auto)
                {
                    pl.Load(auto);
                    Logger.Log(LogType.SystemActivity, "New plugin {0} loaded...build: {1}", pl.name, pl.build);
                }
                else
                {
                    Logger.Log(LogType.SystemActivity, "New plugin {0} was not loaded, you can load it with /newpload", pl.name);
                }

                if (!string.IsNullOrEmpty(pl.welcome)) Logger.Log(LogType.SystemActivity, pl.welcome);
            }
            catch
            {
                if (!string.IsNullOrEmpty(pl.creator)) Logger.Log(LogType.Warning, "You can go bug {0} about {1} failing to load.", pl.creator, pl.name);
                throw;
            }
        }
        public static bool Unload(NewPlugin pl)
        {
            bool success = UnloadNewPlugin(pl, false);

            // TODO only remove if successful?
            custom.Remove(pl);
            core.Remove(pl);
            return success;
        }

        public static bool UnloadNewPlugin(NewPlugin pl, bool auto)
        {
            try
            {
                pl.Unload(auto);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error unloading new plugin " + pl.name, ex);
                return false;
            }
        }
        public static void UnloadAll()
        {
            for (int i = 0; i < custom.Count; i++)
            {
                UnloadNewPlugin(custom[i], true);
            }
            custom.Clear();

            for (int i = 0; i < core.Count; i++)
            {
                UnloadNewPlugin(core[i], true);
            }
        }
        public static void LoadAll()
        {
            LoadNewCorePlugin(new NewCompilerPlugin());
            IScripting.AutoloadNewPlugins();
        }
        public static void LoadNewCorePlugin(NewPlugin newplugin)
        {
            List<string> disabled = Server.Config.DisabledModules;
            if (disabled.CaselessContains(newplugin.name)) return;
            newplugin.Load(true);
            core.Add(newplugin);
        }
    }
    public sealed class NewCompilerPlugin : NewPlugin
    {
        public override string name { get { return "NewCompiler"; } }

        public Command NewCmdCreate = new CmdNewPluginCreate();
        public Command NewCmdCompile = new CmdNewPluginCompile();
        public Command NewCmdCompLoad = new CmdNewPluginCompLoad();

        public override void Load(bool startup)
        {
            Server.EnsureDirectoryExists(ICompiler.NEW_PLUGINS_SOURCE_DIR);
            RegisterCmds(NewCmdCreate,NewCmdCompile, NewCmdCompLoad);
        }

        public override void Unload(bool shutdown)
        {
            Command.Unregister(NewCmdCreate, NewCmdCompile, NewCmdCompLoad);
        }
    }
    public class CmdNewPluginCompile : Command2
    {
        public override string name { get { return "NewPluginCompile"; } }
        public override string shortcut { get { return "NewPCompile"; } }
        public override string type { get { return CommandTypes.Other; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Owner; } }
        public override bool MessageBlockRestricted { get { return true; } }
        public override void Use(Player p, string message, CommandData data)
        {
            string[] args = message.SplitSpaces();
            string name, lang;
            // compile [name] <language>
            name = args[0];
            lang = args.Length > 1 ? args[1] : "";
            if (name.Length == 0)
            {
                Help(p);
                return;
            }
            if (!Formatter.ValidFilename(p, name)) return;
            ICompiler compiler = CompilerOperations.GetCompiler(p, lang);
            if (compiler == null) return;
            // either "source" or "source1,source2,source3"
            string[] paths = name.SplitComma();
            CompileNewPlugin(p, paths, compiler);
        }
        public virtual void CompileNewPlugin(Player p, string[] paths, ICompiler compiler)
        {
            string dstPath = IScripting.NewPluginPath(paths[0]);
            for (int i = 0; i < paths.Length; i++)
            {
                paths[i] = compiler.NewPluginPath(paths[i]);
            }
            CompilerOperations.Compile(p, compiler, "NewPlugin", paths, dstPath);
        }

        public override void Help(Player p)
        {
            ICompiler compiler = ICompiler.Compilers[0];
            p.Message("&T/NewPluginCompile [plugin1 name]");
            p.Message("&HCompiles a .cs file containing a C# new plugin into a DLL");
            p.Message("&H  Compiles from &f{0}", compiler.NewPluginPath("&H<name>&f"));
        }
    }
    public sealed class CmdNewPluginCompLoad : CmdNewPluginCompile
    {
        public override string name { get { return "NewPluginCompLoad"; } }
        public override string shortcut { get { return "NewPCompLoad"; } }
        public override CommandAlias[] Aliases { get { return null; } }

        public override void CompileNewPlugin(Player p, string[] paths, ICompiler compiler)
        {
            string dst = IScripting.NewPluginPath(paths[0]);
            UnloadNewPlugin(p, paths[0]);
            base.CompileNewPlugin(p, paths, compiler);
            ScriptingOperations.LoadNewPlugins(p, dst);
        }
        public static void UnloadNewPlugin(Player p, string name)
        {
            NewPlugin newplugin = NewPlugin.FindCustom(name);

            if (newplugin == null) return;
            ScriptingOperations.UnloadNewPlugin(p, newplugin);
        }
        public override void Help(Player p)
        {
            p.Message("&T/NewPluginCompLoad [new plugin]");
            p.Message("&HCompiles and loads (or reloads) a C# new plugin into the server");
        }
    }
    public sealed class CmdNewPlugin : Command2
    {
        public override string name { get { return "NewPlugin"; } }
        public override string type { get { return CommandTypes.Other; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Owner; } }
        public override CommandAlias[] Aliases
        {
            get
            {
                return new[] { new CommandAlias("NewPLoad", "load"), new CommandAlias("NewPUnload", "unload"),
                    new CommandAlias("NewPlugins", "list") };
            }
        }
        public override bool MessageBlockRestricted { get { return true; } }
        public override void Use(Player p, string message, CommandData data)
        {
            string[] args = message.SplitSpaces(2);
            if (IsListAction(args[0]))
            {
                string modifier = args.Length > 1 ? args[1] : "";

                p.Message("Loaded new plugins:");
                Paginator.Output(p, NewPlugin.custom, pl => pl.name,
                                 "NewPlugins", "New plugins", modifier);
                return;
            }
            if (args.Length == 1)
            {
                Help(p);
                return;
            }
            string cmd = args[0], name = args[1];
            if (!Formatter.ValidFilename(p, name)) return;
            if (cmd.CaselessEq("load"))
            {
                string path = IScripting.NewPluginPath(name);
                ScriptingOperations.LoadNewPlugins(p, path);
            }
            else if (cmd.CaselessEq("unload"))
            {
                UnloadNewPlugin(p, name);
            }
            else if (cmd.CaselessEq("create"))
            {
                Find("NewPluginCreate").Use(p, name);
            }
            else if (cmd.CaselessEq("compile"))
            {
                Find("NewPluginCompile").Use(p, name);
            }
            else
            {
                Help(p);
            }
        }
        public static void UnloadNewPlugin(Player p, string name)
        {
            int matches;
            NewPlugin newplugin = Matcher.Find(p, name, out matches, NewPlugin.custom,
                                         null, pln => pln.name, "New plugins");
            if (newplugin == null) return;
            ScriptingOperations.UnloadNewPlugin(p, newplugin);
        }
        public override void Help(Player p)
        {
            p.Message("&T/NewPlugin load [filename]");
            p.Message("&HLoad a compiled NewPlugin from the &fnew plugins &Hfolder");
            p.Message("&T/NewPlugin unload [name]");
            p.Message("&HUnloads a currently loaded new plugin");
            p.Message("&T/NewPlugin list");
            p.Message("&HLists all loaded new plugins");
        }
    }
    public sealed class CmdNewPluginCreate : CmdNewPluginCompile
    {
        public override string name { get { return "NewPluginCreate"; } }
        public override string shortcut { get { return "NewPCreate"; } }


        public override void CompileNewPlugin(Player p, string[] paths, ICompiler compiler)
        {
            foreach (string cmd in paths)
            {
                CompilerOperations.CreateNewPlugin(p, cmd, compiler);
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/NewPluginCreate [name]");
            p.Message("&HCreate an example C# new plugin named [name]");
        }
    }
    /// <summary> Compiles source code files for a particular programming language into a .dll </summary>
    public abstract class ICompiler
    {
        public const string NEW_PLUGINS_SOURCE_DIR = "newplugins/";
        public const string ERROR_LOG_PATH = "logs/errors/compiler_new.log";

        /// <summary> Default file extension used for source code files </summary>
        /// <example> .cs, .vb </example>
        public abstract string FileExtension { get; }
        /// <summary> The short name of this programming language </summary>
        /// <example> C#, VB </example>
        public abstract string ShortName { get; }
        /// <summary> The full name of this programming language </summary>
        /// <example> CSharp, Visual Basic </example>
        public abstract string FullName { get; }
        /// <summary> Returns source code for an example new plugin </summary>
        public abstract string NewPluginSkeleton { get; }

        public string NewPluginPath(string name) { return NEW_PLUGINS_SOURCE_DIR + name + FileExtension; }

        public static List<ICompiler> Compilers = new List<ICompiler>() {
            new CSCompiler()
        };


        public static string FormatSource(string source, params string[] args)
        {
            // Always use \r\n line endings so it looks correct in Notepad
            source = source.Replace(@"\t", "\t");
            source = source.Replace("\n", "\r\n");
            return string.Format(source, args);
        }

        /// <summary> Generates source code for an example new plugin, 
        /// preformatted with the given name and creator </summary>
        public string GenExampleNewPlugin(string newplugin, string creator)
        {
            return FormatSource(NewPluginSkeleton, newplugin, creator, Server.Version);
        }


        /// <summary> Attempts to compile the given source code files to a .dll file. </summary>
        /// <param name="logErrors"> Whether to log compile errors to ERROR_LOG_PATH </param>
        public ICompilerErrors Compile(string[] srcPaths, string dstPath, bool logErrors)
        {
            ICompilerErrors errors = DoCompile(srcPaths, dstPath);
            if (!errors.HasErrors || !logErrors) return errors;

            SourceMap sources = new SourceMap(srcPaths);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("############################################################");
            sb.AppendLine("Errors when compiling " + srcPaths.Join());
            sb.AppendLine("############################################################");
            sb.AppendLine();

            foreach (ICompilerError err in errors)
            {
                string type = err.IsWarning ? "Warning" : "Error";
                sb.AppendLine(DescribeError(err, srcPaths, "") + ":");

                if (err.Line > 0) sb.AppendLine(sources.Get(err.FileName, err.Line - 1));
                if (err.Column > 0) sb.Append(' ', err.Column - 1);
                sb.AppendLine("^-- " + type + " #" + err.ErrorNumber + " - " + err.ErrorText);

                sb.AppendLine();
                sb.AppendLine("-------------------------");
                sb.AppendLine();
            }

            using (StreamWriter w = new StreamWriter(ERROR_LOG_PATH, true))
            {
                w.Write(sb.ToString());
            }
            return errors;
        }

        public static string DescribeError(ICompilerError err, string[] srcs, string text)
        {
            string type = err.IsWarning ? "Warning" : "Error";
            string file = Path.GetFileName(err.FileName);

            // Include filename if compiling multiple source code files
            return string.Format("{0}{1}{2}{3}", type, text,
                                 err.Line > 0 ? " on line " + err.Line : "",
                                 srcs.Length > 1 ? " in " + file : "");
        }


        /// <summary> Compiles the given source code. </summary>
        public abstract ICompilerErrors DoCompile(string[] srcPaths, string dstPath);


        /// <summary> Converts source file paths to full paths, 
        /// then returns list of parsed referenced assemblies </summary>
        public List<string> ProcessInput(string[] srcPaths, string commentPrefix)
        {
            List<string> referenced = new List<string>();

            for (int i = 0; i < srcPaths.Length; i++)
            {
                // CodeDomProvider doesn't work properly with relative paths
                string path = Path.GetFullPath(srcPaths[i]);

                AddReferences(path, commentPrefix, referenced);
                srcPaths[i] = path;
            }

            referenced.Add(Server.GetServerDLLPath());
            return referenced;
        }

        public void AddReferences(string path, string commentPrefix, List<string> referenced)
        {
            // Allow referencing other assemblies using '//reference [assembly name]' at top of the file
            using (StreamReader r = new StreamReader(path))
            {
                string refPrefix = commentPrefix + "reference ";
                string plgPrefix = commentPrefix + "pluginref ";
                string plgPrefix1 = commentPrefix + "newpluginref ";
                string line;

                while ((line = r.ReadLine()) != null)
                {
                    if (line.CaselessStarts(refPrefix))
                    {
                        referenced.Add(GetDLL(line));
                    }
                    else if (line.CaselessStarts(plgPrefix))
                    {
                        path = Path.Combine(Scripting.IScripting.PLUGINS_DLL_DIR, GetDLL(line));
                        referenced.Add(Path.GetFullPath(path));
                    }
                    else if (line.CaselessStarts(plgPrefix1))
                    {
                        path = Path.Combine(IScripting.NEW_PLUGINS_DLL_DIR, GetDLL(line));
                        referenced.Add(Path.GetFullPath(path));
                    }
                    else
                    {
                        ProcessInputLine(line, referenced);
                    }
                }
            }
        }

        public virtual void ProcessInputLine(string line, List<string> referenced) { }

        public static string GetDLL(string line)
        {
            int index = line.IndexOf(' ') + 1;
            // For consistency with C#, treat '//reference X.dll;' as '//reference X.dll'
            return line.Substring(index).Replace(";", "");
        }
    }

    public class ICompilerErrors : List<ICompilerError>
    {
        public bool HasErrors
        {
            get { return FindIndex(ce => !ce.IsWarning) >= 0; }
        }
    }

    public class ICompilerError
    {
        public int Line, Column;
        public string ErrorNumber, ErrorText;
        public bool IsWarning;
        public string FileName;
    }


    public class SourceMap
    {
        public string[] files;
        public List<string>[] sources;

        public SourceMap(string[] paths)
        {
            files = paths;
            sources = new List<string>[paths.Length];
        }

        public int FindFile(string file)
        {
            for (int i = 0; i < files.Length; i++)
            {
                if (file.CaselessEq(files[i])) return i;
            }
            return -1;
        }

        /// <summary> Returns the given line in the given source code file </summary>
        public string Get(string file, int line)
        {
            int i = FindFile(file);
            if (i == -1) return "";

            List<string> source = sources[i];
            if (source == null)
            {
                try
                {
                    source = Utils.ReadAllLinesList(file);
                }
                catch
                {
                    source = new List<string>();
                }
                sources[i] = source;
            }
            return line < source.Count ? source[line] : "";
        }
    }
    /// <summary> Compiles C# source files into a .dll by invoking a compiler executable directly </summary>
    public abstract class CommandLineCompiler
    {
        public ICompilerErrors Compile(string[] srcPaths, string dstPath, List<string> referenced)
        {
            string args = GetCommandLineArguments(srcPaths, dstPath, referenced);
            string exe = GetExecutable();

            ICompilerErrors errors = new ICompilerErrors();
            List<string> output = new List<string>();
            int retValue = Compile(exe, GetCompilerArgs(exe, args), output);

            // Only look for errors/warnings if the compile failed
            // TODO still log warnings anyways error when success?
            if (retValue != 0)
            {
                foreach (string line in output)
                {
                    ProcessCompilerOutputLine(errors, line);
                }
            }
            return errors;
        }


        public virtual string GetCommandLineArguments(string[] srcPaths, string dstPath,
                                                         List<string> referencedAssemblies)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("/t:library ");

            sb.Append("/utf8output /noconfig /fullpaths ");

            AddCoreAssembly(sb);
            AddReferencedAssemblies(sb, referencedAssemblies);
            sb.AppendFormat("/out:{0} ", Quote(dstPath));

            sb.Append("/D:DEBUG /debug+ /optimize- ");
            sb.Append("/warnaserror- /unsafe ");

            foreach (string path in srcPaths)
            {
                sb.AppendFormat("{0} ", Quote(path));
            }
            return sb.ToString();
        }

        public virtual void AddCoreAssembly(StringBuilder sb)
        {
            string coreAssemblyFileName = typeof(object).Assembly.Location;

            if (!string.IsNullOrEmpty(coreAssemblyFileName))
            {
                sb.Append("/nostdlib+ ");
                sb.AppendFormat("/R:{0} ", Quote(coreAssemblyFileName));
            }
        }

        public abstract void AddReferencedAssemblies(StringBuilder sb, List<string> referenced);

        public static string Quote(string value) { return "\"" + value.Trim() + "\""; }

        public abstract string GetExecutable();
        public abstract string GetCompilerArgs(string exe, string args);


        public static int Compile(string path, string args, List<string> output)
        {
            // https://stackoverflow.com/questions/285760/how-to-spawn-a-process-and-capture-its-stdout-in-net
            ProcessStartInfo psi = CreateStartInfo(path, args);

            using (Process p = new Process())
            {
                p.OutputDataReceived += (s, e) => { if (e.Data != null) output.Add(e.Data); };
                p.ErrorDataReceived += (s, e) => { }; // swallow stderr output

                p.StartInfo = psi;
                p.Start();

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                if (!p.WaitForExit(120 * 1000))
                    throw new InvalidOperationException("C# compiler ran for over two minutes! Giving up..");

                return p.ExitCode;
            }
        }

        public static ProcessStartInfo CreateStartInfo(string path, string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo(path, args);
            psi.WorkingDirectory = Environment.CurrentDirectory;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            return psi;
        }


        public static Regex outputRegWithFileAndLine;
        public static Regex outputRegSimple;

        public static void ProcessCompilerOutputLine(ICompilerErrors errors, string line)
        {
            if (outputRegSimple == null)
            {
                outputRegWithFileAndLine =
                    new Regex(@"(^(.*)(\(([0-9]+),([0-9]+)\)): )(error|warning) ([A-Z]+[0-9]+) ?: (.*)");
                outputRegSimple =
                    new Regex(@"(error|warning) ([A-Z]+[0-9]+) ?: (.*)");
            }

            //First look for full file info
            Match m = outputRegWithFileAndLine.Match(line);
            bool full;
            if (m.Success)
            {
                full = true;
            }
            else
            {
                m = outputRegSimple.Match(line);
                full = false;
            }

            if (!m.Success) return;
            ICompilerError ce = new ICompilerError();

            if (full)
            {
                ce.FileName = m.Groups[2].Value;
                ce.Line = NumberUtils.ParseInt32(m.Groups[4].Value);
                ce.Column = NumberUtils.ParseInt32(m.Groups[5].Value);
            }

            ce.IsWarning = m.Groups[full ? 6 : 1].Value.CaselessEq("warning");
            ce.ErrorNumber = m.Groups[full ? 7 : 2].Value;
            ce.ErrorText = m.Groups[full ? 8 : 3].Value;
            errors.Add(ce);
        }
    }

    public class ClassicCSharpCompiler : CommandLineCompiler
    {
        public override void AddCoreAssembly(StringBuilder sb)
        {
            string coreAssemblyFileName = typeof(object).Assembly.Location;

            if (!string.IsNullOrEmpty(coreAssemblyFileName))
            {
                sb.Append("/nostdlib+ ");
                sb.AppendFormat("/R:{0} ", Quote(coreAssemblyFileName));
            }
        }

        public override void AddReferencedAssemblies(StringBuilder sb, List<string> referenced)
        {
            foreach (string path in referenced)
            {
                sb.AppendFormat("/R:{0} ", Quote(path));
            }
        }


        public override string GetExecutable()
        {
            string root = RuntimeEnvironment.GetRuntimeDirectory();

            string[] paths = new string[] {
                // First try new C# compiler
                Path.Combine(root, "csc.exe"),
                // Then fallback to old Mono C# compiler
                Path.Combine(root, @"../../../bin/mcs"),
                Path.Combine(root, "mcs.exe"),
                "/usr/bin/mcs",
            };

            foreach (string path in paths)
            {
                if (File.Exists(path)) return path;
            }
            return paths[0];
        }

        public override string GetCompilerArgs(string exe, string args)
        {
            return args;
        }
    }
    public sealed class CSCompiler : ICompiler
    {
        public override string FileExtension { get { return ".cs"; } }
        public override string ShortName { get { return "C#"; } }
        public override string FullName { get { return "CSharp"; } }

        public override ICompilerErrors DoCompile(string[] srcPaths, string dstPath)
        {
            List<string> referenced = ProcessInput(srcPaths, "//");

            CommandLineCompiler compiler = new ClassicCSharpCompiler();
            return compiler.Compile(srcPaths, dstPath, referenced);
        }

        public override string NewPluginSkeleton
        {
            get
            {
                return @"//\tAuto-generated new plugin skeleton class
//\tUse this as a basis for custom MCGalaxy new plugins

// To reference other assemblies, put a ""//reference [assembly filename]"" at the top of the file
//   e.g. to reference the System.Data assembly, put ""//reference System.Data.dll""
// You will still have to add ""//reference plugins/NewPlugin.dll"" to the top of the file.
// Add any other using statements you need after this
using System;
using MCGalaxy.New;
namespace MCGalaxy
{{
\tpublic class {0} : NewPlugin
\t{{
\t\t// The plugin1's name (i.e what shows in /NewPlugins)
\t\tpublic override string name {{ get {{ return ""{0}""; }} }}

\t\t// The oldest version of MCGalaxy this new plugin is compatible with
\t\tpublic override string MCGalaxy_Version {{ get {{ return ""{2}""; }} }}

\t\t// Message displayed in server logs when this new plugin is loaded
\t\tpublic override string welcome {{ get {{ return ""Loaded Message!""; }} }}

\t\t// Who created/authored this new plugin
\t\tpublic override string creator {{ get {{ return ""{1}""; }} }}

\t\t// Called when this new plugin is being loaded (e.g. on server startup)
\t\tpublic override void Load(bool startup)
\t\t{{
\t\t\t//code to hook into events, load state/resources etc goes here
\t\t}}

\t\t// Called when this new plugin is being unloaded (e.g. on server shutdown)
\t\tpublic override void Unload(bool shutdown)
\t\t{{
\t\t\t//code to unhook from events, dispose of state/resources etc goes here
\t\t}}

\t\t// Displays help for or information about this new plugin
\t\tpublic override void Help(Player p)
\t\t{{
\t\t\tp.Message(""No help is available for this new plugin."");
\t\t}}
\t}}
}}";
            }
        }
    }
    public static class CompilerOperations
    {
        public static ICompiler GetCompiler(Player p, string name)
        {
            if (name.Length == 0) return ICompiler.Compilers[0];

            foreach (ICompiler comp in ICompiler.Compilers)
            {
                if (comp.ShortName.CaselessEq(name)) return comp;
            }

            p.Message("&WUnknown language \"{0}\"", name);
            p.Message("&HAvailable languages: &f{0}",
                      ICompiler.Compilers.Join(c => c.ShortName + " (" + c.FullName + ")"));
            return null;
        }

        public static bool CreateNewPlugin(Player p, string name, ICompiler compiler)
        {
            string path = compiler.NewPluginPath(name);
            string creator = p.IsSuper ? Colors.Strip(Server.Config.Name) : p.truename;
            string source = compiler.GenExampleNewPlugin(name, creator);

            return CreateFile(p, name, path, "newplugin &f", source);
        }

        public static bool CreateFile(Player p, string name, string path, string type, string source)
        {
            if (File.Exists(path))
            {
                p.Message("File {0} already exists. Choose another name.", path);
                return false;
            }

            File.WriteAllText(path, source);
            p.Message("Successfully saved example {2}{0} &Sto {1}", name, path, type);
            return true;
        }


        /// <summary> Attempts to compile the given source code files into a .dll </summary>
        /// <param name="p"> Player to send messages to </param>
        /// <param name="type"> Type of files being compiled (e.g. new plugin) </param>
        /// <param name="srcs"> Path of the source code files </param>
        /// <param name="dst"> Path to the destination .dll </param>
        /// <returns> Whether compilation succeeded </returns>
        public static bool Compile(Player p, ICompiler compiler, string type, string[] srcs, string dst)
        {
            foreach (string path in srcs)
            {
                if (File.Exists(path)) continue;

                p.Message("File &9{0} &Snot found.", path);
                return false;
            }

            ICompilerErrors errors = compiler.Compile(srcs, dst, true);
            if (!errors.HasErrors)
            {
                p.Message("{0} compiled successfully from {1}",
                        type, srcs.Join(file => Path.GetFileName(file)));
                return true;
            }

            SummariseErrors(errors, srcs, p);
            return false;
        }

        public const int MAX_LOG = 5;
        public static void SummariseErrors(ICompilerErrors errors, string[] srcs, Player p)
        {
            int logged = 0;
            foreach (ICompilerError err in errors)
            {
                p.Message("&W{1} - {0}", err.ErrorText,
                          ICompiler.DescribeError(err, srcs, " #" + err.ErrorNumber));
                logged++;
                if (logged >= MAX_LOG) break;
            }

            if (logged < errors.Count)
            {
                p.Message(" &W.. and {0} more", errors.Count - logged);
            }
            p.Message("&WCompiling failed. See " + ICompiler.ERROR_LOG_PATH + " for more detail");
        }
    }
    /// <summary> Exception raised when attempting to load a new plugin1
    /// that has the same name as an already loaded new plugin </summary>
    public sealed class AlreadyLoadedException : Exception
    {
        public AlreadyLoadedException(string msg) : base(msg) { }
    }

    /// <summary> Utility methods for loading assemblies, and new plugins </summary>
    public static class IScripting
    {
        public static string GetExePath(string path)
        {
            return path;
        }

        public static Assembly ResolveNewPluginReference(string name)
        {
            return null;
        }

        public const string NEW_PLUGINS_DLL_DIR = "newplugins/";

        /// <summary> Returns the default .dll path for the new plugin with the given name </summary>
        public static string NewPluginPath(string name) { return NEW_PLUGINS_DLL_DIR + name + ".dll"; }


        public static void Init()
        {
            Directory.CreateDirectory(NEW_PLUGINS_DLL_DIR);
            AppDomain.CurrentDomain.AssemblyResolve += ResolveNewPluginAssembly;
        }

        // only used for resolving new plugin DLLs depending on other new plugin DLLs
        static Assembly ResolveNewPluginAssembly(object sender, ResolveEventArgs args)
        {
            // This property only exists in .NET framework 4.0 and later
            Assembly requestingAssembly = args.RequestingAssembly;

            if (requestingAssembly == null) return null;
            if (!IsNewPluginDLL(requestingAssembly)) return null;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assem in assemblies)
            {
                if (!IsNewPluginDLL(assem)) continue;

                if (args.Name == assem.FullName) return assem;
            }

            Assembly coreRef = ResolveNewPluginReference(args.Name);
            if (coreRef != null) return coreRef;

            Logger.Log(LogType.Warning, "Custom new plugin [{0}] tried to load [{1}], but it could not be found",
                       requestingAssembly.FullName, args.Name);
            return null;
        }

        static bool IsNewPluginDLL(Assembly a) { return string.IsNullOrEmpty(a.Location); }


        /// <summary> Constructs instances of all types which derive from T in the given assembly. </summary>
        /// <returns> The list of constructed instances. </returns>
        public static List<T> LoadTypes<T>(Assembly lib)
        {
            List<T> instances = new List<T>();

            foreach (Type t in lib.GetTypes())
            {
                if (t.IsAbstract || t.IsInterface || !t.IsSubclassOf(typeof(T))) continue;
                object instance = Activator.CreateInstance(t);

                if (instance == null)
                {
                    Logger.Log(LogType.Warning, "{0} \"{1}\" could not be loaded", typeof(T).Name, t.Name);
                    throw new BadImageFormatException();
                }
                instances.Add((T)instance);
            }
            return instances;
        }

        /// <summary> Loads the given assembly from disc (and associated .pdb debug data) </summary>
        public static Assembly LoadAssembly(string path)
        {
            byte[] data = File.ReadAllBytes(path);
            byte[] debug = GetDebugData(path);
            return Assembly.Load(data, debug);
        }

        static byte[] GetDebugData(string path)
        {
            if (Server.RunningOnMono())
            {
                // test.dll -> test.dll.mdb
                path += ".mdb";
            }
            else
            {
                // test.dll -> test.pdb
                path = Path.ChangeExtension(path, ".pdb");
            }

            if (!File.Exists(path)) return null;
            try
            {
                return File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error loading .pdb " + path, ex);
                return null;
            }
        }


        public static string DescribeLoadError(string path, Exception ex)
        {
            string file = Path.GetFileName(path);

            if (ex is BadImageFormatException)
            {
                return "&W" + file + " is not a valid assembly, or has an invalid dependency. Details in the error log.";
            }
            else if (ex is FileLoadException)
            {
                return "&W" + file + " or one of its dependencies could not be loaded. Details in the error log.";
            }

            return "&WAn unknown error occured. Details in the error log.";
            // p.Message("&WError loading new plugin. See error logs for more information.");
        }


        public static void AutoloadNewPlugins()
        {
            string[] files = AtomicIO.TryGetFiles(NEW_PLUGINS_DLL_DIR, "*.dll");
            if (files == null) return;

            // Ensure that new plugin files are loaded in a consistent order,
            //  in case new plugins have a dependency on other new plugins
            Array.Sort<string>(files);

            foreach (string path in files)
            {
                try
                {
                    LoadNewPlugin(path, true);
                }
                catch (Exception ex)
                {
                    Logger.LogError("Error loading new plugins from " + path, ex);
                }
            }
        }

        /// <summary> Loads all new plugins from the given .dll path. </summary>
        public static List<NewPlugin> LoadNewPlugin(string path, bool auto)
        {
            Assembly lib = LoadAssembly(path);
            List<NewPlugin> newplugins = LoadTypes<NewPlugin>(lib);

            foreach (NewPlugin pl in newplugins)
            {
                if (NewPlugin.FindCustom(pl.name) != null)
                    throw new AlreadyLoadedException("NewPlugin " + pl.name + " is already loaded");

                NewPlugin.Load(pl, auto);
            }
            return newplugins;
        }
    }
    public static class ScriptingOperations
    {
        public static bool LoadNewPlugins(Player p, string path)
        {
            if (!File.Exists(path))
            {
                p.Message("File &9{0} &Snot found.", path);
                return false;
            }

            try
            {
                List<NewPlugin> newplugins = IScripting.LoadNewPlugin(path, false);

                p.Message("New plugin {0} loaded successfully",
                          newplugins.Join(pl => pl.name));
                return true;
            }
            catch (AlreadyLoadedException ex)
            {
                p.Message(ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                p.Message(IScripting.DescribeLoadError(path, ex));
                Logger.LogError("Error loading new plugins from " + path, ex);
                return false;
            }
        }

        public static bool UnloadNewPlugin(Player p, NewPlugin newplugin)
        {
            if (!NewPlugin.Unload(newplugin))
            {
                p.Message("&WError unloading new plugin. See error logs for more information.");
                return false;
            }

            p.Message("New plugin {0} &Sunloaded successfully", newplugin.name);
            return true;
        }
    }
}