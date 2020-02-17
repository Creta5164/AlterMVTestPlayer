using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using ToolBox.Bridge;
using ToolBox.Platform;

namespace AlterMVTestPlayer
{
    class Program
    {
        public const string NW_DIR = "nwjs";
        public const string NWL_DIR = "nwjs_legacy";
        public const string ZIP = ".zip";
        public const string NW_ZIP = NW_DIR + ZIP;
        public const string NWL_ZIP = NWL_DIR + ZIP;
        public const string NW_EXE = "nw.exe";
        public const string NW_APP = "nw.app";
        public static ShellConfigurator Shell { get; private set; }

        public static string ExecutionFile          { get; private set; }
        public static string ProcessPath            { get; private set; }
        public static string TargetProject          { get; private set; }
        public static string TargetProjectDirectory { get; private set; }
        public static string TargetProjectEntry     { get; private set; }
        public static Architecture Architecture     { get; private set; }
        
        public static string NWPath
            => Path.Combine(Program.ProcessPath, OldVersion ? Program.NWL_DIR : Program.NW_DIR);
        public static string NWZip
            => Path.Combine(Program.ProcessPath, OldVersion ? Program.NWL_ZIP : Program.NW_ZIP);
        
        public static string NWExe => Path.Combine(NWPath, NW_EXE);
        public static string NWApp => Path.Combine(NWPath, NW_APP);
        public static object BulitInNWExe => Path.Combine(Directory.GetCurrentDirectory(), "nwjs-win-test", "Game.exe");
        public static object BulitInNWApp => Path.Combine(Directory.GetCurrentDirectory(), "nwjs-osx-test", "Game.app");
        
        public static bool TestMode   { get; private set; }
        public static bool OldVersion { get; private set; }

        static int Main(string[] args)
        {
            if (args.Length == 0) {
                
                TerminateProcess("파라미터가 없습니다.");
                return 4;
            }
            
            TestMode = false;
            
            if (args[0] == "-t") {
                
                TestMode = true;
                args = args.Skip(1).ToArray();
            }
            
            TargetProject = string.Empty;
            foreach (var arg in args) {
                
                    switch (arg) {
                        
                        default:
                            TargetProject += arg + ' ';
                            break;
                        
                        case "-t":
                            TestMode = true;
                            break;
                        
                        case "-o":
                            OldVersion = true;
                            break;
                            
                    }
            }
            
            TargetProject          = TargetProject.Substring(0, TargetProject.Length - 1);
            
            ExecutionFile          = Process.GetCurrentProcess().MainModule.FileName;
            //ExecutionFile          = System.Reflection.Assembly.GetEntryAssembly().Location;
            ProcessPath            = Path.GetDirectoryName(ExecutionFile);
            TargetProjectDirectory = Path.GetDirectoryName(TargetProject);
            TargetProjectEntry     = Path.Combine(TargetProjectDirectory, "index.html");
            
            Program.Architecture = RuntimeInformation.OSArchitecture;
            
            if (!File.Exists(TargetProject)) {
                
                TerminateProcess($"프로젝트 파일 ('{TargetProject.Replace('\\', '/')}')을 찾을 수 없습니다.\n(혹시 프로젝트 폴더 경로에 띄어쓰기가 '두번 이상' 들어간 이름이 있는지 확인하세요.)");
                return 1;
            }

            ConsoleKeyInfo key;
            
            Console.WriteLine("테스트를 하기 위한 환경을 결정합니다.");
            Console.WriteLine("테스트 모드를 사용할까요? (Y / N)");
            Console.WriteLine("(입력하지 않고 엔터 : 사용)");
            
            while (true) {
                
                key = Console.ReadKey();
                
                if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Y) {
                    
                    TestMode = true;
                    break;
                }
                
                else if (key.Key == ConsoleKey.N) {
                    
                    TestMode = false;
                    break;
                }
                
                Console.WriteLine();
            }
            
            Console.WriteLine();
            Console.WriteLine();
            
            Console.WriteLine("테스트 모드 : " + (TestMode ? "사용" : "사용 안 함"));
            Console.WriteLine();
            
            Console.WriteLine("구형 노드웹킷으로 실행할까요? (Y / N)");
            Console.WriteLine("(입력하지 않고 엔터 : 사용 안 함)");
            
            while (true) {
                
                key = Console.ReadKey();
                
                if (key.Key == ConsoleKey.Y) {
                    
                    OldVersion = true;
                    break;
                }
                
                else if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.N) {
                    
                    OldVersion = false;
                    break;
                }
            
                Console.WriteLine();
            }
            
            Console.WriteLine();
            Console.WriteLine();
            
            Console.WriteLine("구형 노드웹킷 : " + (OldVersion ? "사용" : "사용 안 함"));
            
            Console.WriteLine();
            
            Console.WriteLine("            운영체제 : " + OS.GetCurrent());
            Console.WriteLine("         실행한 위치 : " + Path.GetFullPath("."));
            Console.WriteLine("       실행중인 파일 : " + ExecutionFile);
            Console.WriteLine("실행중인 파일의 경로 : " + ProcessPath);
            Console.WriteLine("실행할 프로젝트 파일 : " + TargetProject);
            Console.WriteLine("            실행환경 : " + (TestMode   ? "테스트 모드" : "릴리즈 모드"));
            Console.WriteLine("          클라이언트 : " + (OldVersion ? "레거시 노드 웹킷 (0.12.3)" : "스탠다드 노드 웹킷 (0.44.1)"));
            
            Console.WriteLine();
            
            try {
                
                switch (OS.GetCurrent()) {
                    
                    case "win":
                        
                        Shell = new ShellConfigurator(BridgeSystem.Bat);
                        return WindowsRunner.Run();
                    
                    case "mac":
                    
                        Shell = new ShellConfigurator(BridgeSystem.Bash);
                        return OSXRunner.Run();
                    
                    default:
                    case "gnu":
                        
                        TerminateProcess("리눅스 플랫폼은 지원하지 않습니다.");
                        return 2;
                }
                
            } catch (Exception ex) {
                
                TerminateProcessWithException(ex);
                return 6;
            }
        }
        
        public static void TerminateProcessWithException(Exception exception) {
            
            Console.WriteLine("프로그램에서 예기치 못한 문제가 발생하였습니다, 프로그램을 중단합니다.");
            Console.WriteLine("===메세지===");
            Console.WriteLine(exception.Message);
            Console.WriteLine("===스택 트레이스===");
            Console.WriteLine(exception.StackTrace);
            Console.WriteLine("프로그램을 종료하려면 아무키나 입력하세요.");
            Console.ReadKey();
        }

        public static void TerminateProcess(string v) {
            
            Console.WriteLine(v);
            Console.WriteLine("프로그램을 종료하려면 아무키나 입력하세요.");
            Console.ReadKey();
        }
    }
}
