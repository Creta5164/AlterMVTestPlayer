using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using ToolBox.Bridge;

namespace AlterMVTestPlayer {
    
    public static class OSXRunner {
        
        public static int Run() {
            
            if (!Directory.Exists(Program.NWPath)) {
                
                if (!DownloadNodeWebkit())
                    return 5;
            }
            
            string executionLine;
            
            if (Program.TestMode) {
                
                Console.WriteLine("쯔구르 게임을 디버그 모드로 실행하는 중...");
                
                if (Program.OldVersion)
                    executionLine = $"\"{Program.NWApp}\" --nwapp --url=\"{Program.TargetProjectEntry}?test\"";
                
                else
                    executionLine = $"\"{Program.BulitInNWExe}\" \"{Program.TargetProjectDirectory}\" test";
                
            } else {
                
                Console.WriteLine("쯔구르 게임을 릴리즈 모드로 실행하는 중...");
                
                if (Program.OldVersion)
                    executionLine = $"\"{Program.NWApp}\" --nwapp --url=\"{Program.TargetProjectEntry}\"";
                
                else
                    executionLine = $"\"{Program.BulitInNWExe}\" \"{Program.TargetProjectDirectory}\"";
            }
            
            Console.WriteLine(executionLine);
            Console.WriteLine();
            
            Program.Shell.Term(executionLine, Output.Internal);
            Console.WriteLine();
            
            Console.WriteLine("프로그램이 종료되었습니다. (3초 뒤 자동으로 닫음)");
            Thread.Sleep(3000);
            
            return 0;
        }
        
        static volatile bool DownloadLocker;
        static ProgressBar DownloadProgress;
        private static bool DownloadNodeWebkit()
        {
            string packagePath = Path.Combine(Program.ProcessPath, Program.NWZip);
            string DownloadTarget = string.Empty;
            
            switch (Program.Architecture) {
                
                default:
                    
                    Program.TerminateProcess("지원하지 않는 프로세서(ARM)입니다.");
                    return false;
                    
                case Architecture.X86:
                    
                    if (Program.OldVersion)
                        DownloadTarget = @"https://dl.nwjs.io/v0.12.3/nwjs-v0.12.3-osx-ia32.zip";
                    
                    else {
                        
                        Program.TerminateProcess("신형 노드 웹킷은 32비트 프로세서를 지원하지 않습니다.");
                        return false;
                    }
                    
                    
                    break;
                    
                case Architecture.X64:
                    
                    if (Program.OldVersion)
                        DownloadTarget = @"https://dl.nwjs.io/v0.12.3/nwjs-v0.12.3-osx-x64.zip";
                    
                    else {
                        
                        DownloadTarget = @"https://dl.nwjs.io/v0.44.1/nwjs-v0.44.1-osx-x64.zip";
                    }
                    
                    break;
            }
            
            Console.WriteLine("게임을 실행하기 위해 필요한 파일을 다운로드 합니다. (첫 1회 실행 시)");
            Console.WriteLine();
            Console.WriteLine($"'{DownloadTarget}' 에서");
            Console.Write("게임을 실행하기 위한 클라이언트(node-webkit)를 다운로드 하는 중... ");
            DownloadProgress = new ProgressBar();
            using (var client = new WebClient()) {
                
                client.DownloadProgressChanged += DownloadProgressChanged;
                client.Disposed += DownloadClientDisposed;
                client.DownloadFileCompleted += DownloadFileCompleted;
                
                if (File.Exists(packagePath))
                    File.Delete(packagePath);
                
                client.DownloadFileAsync(new Uri(DownloadTarget), packagePath);
                
                while (!DownloadLocker) ;
                
                DownloadProgress.Dispose();
            }
            
            Console.WriteLine("완료!");
            Console.WriteLine();
            
            Thread.Sleep(1000);
            
            ZipFile.ExtractToDirectory(packagePath, Program.ProcessPath, true);
            
            if (File.Exists(packagePath))
                File.Delete(packagePath);
            
            string extractPath = Path.GetFileNameWithoutExtension(DownloadTarget);
            extractPath = Path.Combine(Program.ProcessPath, extractPath);
            
            if (Directory.Exists(Program.NWPath))
                Directory.Delete(Program.NWPath);
            
            Directory.Move(extractPath, Program.NWPath);
            
            return true;
        }

        private static void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
            => DownloadLocker = true;

        private static void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e) {
            
            if (e.ProgressPercentage == 100)
                DownloadLocker = true;
            
            DownloadProgress.Report(e.ProgressPercentage / 100d);
        }

        private static void DownloadClientDisposed(object sender, EventArgs e)
            => (sender as WebClient).DownloadProgressChanged -= DownloadProgressChanged;
    }
}