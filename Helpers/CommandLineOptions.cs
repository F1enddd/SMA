using CommandLine;
using CommandLine.Text;

namespace SMA.Helpers
{
    class CommandLineOptions
    {
        [Option('k', "encryption-key", Required = false,
            HelpText = "Encryption key for manifest")]
        public string EncryptionKey { get; set; }

        [Option('s', "silent", Required = false,
            HelpText = "Start minimized")]
        public bool Silent { get; set; }
    }
}