using FreeSql.DataAnnotations;

namespace Cola.Model
{
    [Table(Name = "check_para")]
    public class CheckItem
    {
        [Column(IsPrimary = true)]
        public int id { get; set; }
        public int no { get; set; }
        public string alias_name { get; set; }
        public string name { get; set; }
        public int device_id { get; set; }
        public string unit { get; set; }
    }
}
