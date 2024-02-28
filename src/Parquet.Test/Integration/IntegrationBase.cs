using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Parquet.Test.Integration {
    public class IntegrationBase : TestBase {
        private readonly string _toolsPath;
        private readonly string _toolsJarPath;
        private readonly string _javaExecName;
        private readonly string _commandLineName;
        private readonly string _pythonExecName;
        private readonly string _pipExecName;

        public IntegrationBase() {
            _toolsPath = Path.GetFullPath(Path.Combine("..", "..", "..", "..", "..", "tools"));
            _toolsJarPath = Path.Combine(_toolsPath, "parquet-tools-1.9.0.jar");

            _javaExecName = Environment.OSVersion.Platform == PlatformID.Win32NT
               ? "java.exe"
               : "java";

            _commandLineName = Environment.OSVersion.Platform == PlatformID.Win32NT
                ? "cmd"
                : "bash";
            
            _pythonExecName = Environment.OSVersion.Platform == PlatformID.Win32NT
                ? "python"
                : "python3";
            
            _pipExecName = Environment.OSVersion.Platform == PlatformID.Win32NT
                ? "pip"
                : "pip3";
        }

        private string? ExecAndGetOutput(string execPath, string arguments, params string[] commands) {
            var psi = new ProcessStartInfo {
                FileName = execPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var proc = new Process { StartInfo = psi };

            if(!proc.Start())
                return null;
            
            using StreamWriter sw = proc.StandardInput;
            if(sw.BaseStream.CanWrite) {
                foreach(string command in commands) {
                    sw.WriteLine(command);
                }
                sw.Flush();
                sw.Close();
            }

            var so = new StringBuilder();
            var se = new StringBuilder();

            while(!proc.StandardOutput.EndOfStream) {
                string? line = proc.StandardOutput.ReadLine();
                if(line != null) {
                    so.AppendLine(line);
                }
            }

            while(!proc.StandardError.EndOfStream) {
                string? line = proc.StandardError.ReadLine();
                if(line != null) {
                    se.AppendLine(line);
                }
            }

            proc.WaitForExit();

            if(proc.ExitCode != 0) {
                throw new Exception("process existed with code " + proc.ExitCode + ", error: " + se.ToString());
            }

            return so.ToString().Trim();
        }

        private string? ExecJavaAndGetOutput(string arguments) {
            return ExecAndGetOutput(_javaExecName, arguments);
        }

        private string? ExecPythonAndGetOutput(string arguments) {
            string[] commands = [
                $"{_pythonExecName} -m venv .venv",
                Path.Combine(".venv", "Scripts", "activate"),
                $"{_pipExecName} install -r {Path.Combine("Integration", "requirements.txt")}",
                $"{_pythonExecName} {arguments}"
            ];
            return ExecAndGetOutput(_commandLineName, "", commands);
        }

        protected string? ExecMrCat(string testFileName) {
            return ExecJavaAndGetOutput($"-jar \"{_toolsJarPath}\" cat -j \"{testFileName}\"");
        }

        protected string? ExecPyArrowToJson(string testFileName) {
            return ExecPythonAndGetOutput($"Integration/pyarrow_to_json.py \"{testFileName}\"");
        }
    }
}
