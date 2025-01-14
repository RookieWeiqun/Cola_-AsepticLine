using FreeSql.DataAnnotations;
using Newtonsoft.Json;

namespace Cola.Model
{

    [JsonObject(MemberSerialization.OptIn), Table(Name = "check_para", DisableSyncStructure = true)]
	public partial class CheckPara {

		[JsonProperty, Column(Name = "id", IsPrimary = true, IsIdentity = true, InsertValueSql = "nextval('check_para_id_seq'::regclass)")]
		public int Id { get; set; }

		[JsonProperty, Column(Name = "addr")]
		public string Addr { get; set; }

		[JsonProperty, Column(Name = "alias_name")]
		public string AliasName { get; set; }

		[JsonProperty, Column(Name = "device_id")]
		public int? DeviceId { get; set; }

		[JsonProperty, Column(Name = "name", StringLength = 50)]
		public string Name { get; set; }

		[JsonProperty, Column(Name = "no")]
		public short? No { get; set; }

		[JsonProperty, Column(Name = "unit", StringLength = 10)]
		public string Unit { get; set; }

	}

}
