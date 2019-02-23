using IniParser;
using IniParser.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace RegenCredentials
{
    class Program
    {
        const string FILENAME = "credentials";
        const string MASTER = "master";
        const string REGION = "eu-west-2"; // Could load this from profile

        static void Main(string[] args)
        {
            var parser = new FileIniDataParser();

            IniData ini;
            if (File.Exists(FILENAME))
                ini = parser.ReadFile(FILENAME);
            else
                ini = new IniData();

            var mfaDevice = LoadOrGetInput(ini[MASTER], "MFA Device");

            var profile = (args.Length > 1) ? args[1] : "default";
            var credentialSection = ini[profile];

            var code = GetInput("Token Code");

            var data = Run(mfaDevice, code);

            dynamic info = JObject.Parse(data);

            credentialSection["aws_access_key_id"] = info.Credentials.AccessKeyId;
            credentialSection["aws_secret_access_key"] = info.Credentials.SecretAccessKey;
            credentialSection["aws_session_token"] = info.Credentials.SessionToken;

            parser.WriteFile(FILENAME, ini, new UTF8Encoding(false));

            Console.WriteLine("Successful. Token Expires: " + info.Credentials.Expiration);
        }

        static string Run(string device, string code)
        {
            var command = $"/C aws --profile {MASTER} --region {REGION} sts get-session-token --token-code {code} --serial-number {device}";

            var cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.Arguments = command;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.RedirectStandardError = true;
            cmd.Start();

            cmd.WaitForExit();

            var err = cmd.StandardError.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(err))
                throw new Exception(err);

            return cmd.StandardOutput.ReadToEnd();
        }

        static string LoadOrGetInput(KeyDataCollection section, string request)
        {
            var key = EscapeString(request);
            var x = section[key];
            if (x != null)
                return x;

            x = GetInput(request);
            section[key] = x;
            return x;
        }

        static string GetInput(string request)
        {
            Console.WriteLine("Please provide your " + request);
            while (true)
            {
                var x = Console.ReadLine();
                if (x.Length > 0)
                {
                    return x;
                }
            }
        }

        static string EscapeString(string s)
        {
            var x = s.ToLower().Replace(" ", "_");
            return char.ToLowerInvariant(x[0]) + x.Substring(1);
        }
    }
}
