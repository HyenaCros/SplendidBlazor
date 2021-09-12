using System;
using System.ComponentModel.DataAnnotations;

namespace Shared
{
    public class RazorFile
    {
        [Key]
        public Guid Id { get; set; }
        public string ClassName { get; set; }
        public string Contents { get; set; }
        public int Version {  get; set; }
    }
}
