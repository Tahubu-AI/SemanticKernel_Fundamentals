using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.VisualBasic;

namespace MyPlugins
{
    public class PersonalInfo
    {
        [KernelFunction("get_my_information")]
        [Description("call this when my information is needed including name, address, location, Birthplace or birthdate")]
        [return: Description("returns my name, address, location, birthplace and birthdate formated as JSON")]
        public Info GetInfo() => new Info();
    }


    public class Info
    {
        public string Name { get => "Lino Tadros"; }
        public DateTime Birthdate { get => new DateTime(1971, 2, 8); }
        public string Address { get => "Orlando, FL"; }
        public string Birthplace { get => "Alexandria, Egypt"; }
    }
}
