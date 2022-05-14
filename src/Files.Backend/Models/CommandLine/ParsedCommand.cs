using Files.Backend.Enums;

namespace Files.Backend.Models.CommandLine
{
    public class ParsedCommand
    {
        public ParsedCommandType Type { get; set; }

        public string Payload { get; set; }
    }
}