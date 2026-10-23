using System;
using System.IO;

namespace AndroidLive.Setup
{
    internal enum SetupAction { Interactive, Install, Uninstall, Detect, Verify }
    internal sealed class CommandLine
    {
        internal SetupAction Action;
        internal bool Silent;
        internal string LogPath,VerifyPath;
        internal static CommandLine Parse(string[] args)
        {
            if(args.Length==0)return new CommandLine{Action=SetupAction.Interactive};
            if(args.Length==2&&args[0]=="--verify-package"&&!args[1].StartsWith("--"))
                return new CommandLine{Action=SetupAction.Verify,VerifyPath=args[1]};
            if(args.Length==1&&args[0]=="--detect")return new CommandLine{Action=SetupAction.Detect,Silent=true};
            var result=new CommandLine();bool action=false,silent=false,log=false;
            for(int i=0;i<args.Length;i++) {
                if((args[i]=="--install"||args[i]=="--uninstall")&&!action) {
                    result.Action=args[i]=="--install"?SetupAction.Install:SetupAction.Uninstall;action=true;
                } else if(args[i]=="--silent"&&!silent){result.Silent=true;silent=true;}
                else if(args[i]=="--log"&&!log&&i+1<args.Length&&!args[i+1].StartsWith("--")) {
                    result.LogPath=Path.GetFullPath(args[++i]);log=true;
                } else throw new ArgumentException("Ugyldige installationsparametre.");
            }
            if(!action||!silent)throw new ArgumentException("IT-installation kræver --install/--uninstall og --silent.");
            return result;
        }
    }
    internal sealed class SetupFailure : Exception
    {
        internal readonly int ExitCode;
        internal SetupFailure(int code,string message):base(message){ExitCode=code;}
    }
}