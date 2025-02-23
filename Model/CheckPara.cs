using FreeSql.DatabaseModel;using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using FreeSql.DataAnnotations;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Net.NetworkInformation;
using NpgsqlTypes;
using Npgsql.LegacyPostgis;

namespace Cola.Model {

	[JsonObject(MemberSerialization.OptIn), Table(Name = "check_para", DisableSyncStructure = false)]
	public partial class CheckPara {

		[JsonProperty, Column(Name = "id", IsPrimary = true, IsIdentity = true, InsertValueSql = "nextval('check_para_id_seq'::regclass)")]
		public int Id { get; set; }

		[JsonProperty, Column(Name = "addr")]
		public string Addr { get; set; }

		[JsonProperty, Column(Name = "alias_name")]
		public string AliasName { get; set; }

		[JsonProperty, Column(Name = "checked")]
		public short? Checked { get; set; }

		[JsonProperty, Column(Name = "counted")]
		public short? Counted { get; set; }

		[JsonProperty, Column(Name = "device_id")]
		public int? DeviceId { get; set; }

		[JsonProperty, Column(Name = "name", StringLength = 50)]
		public string Name { get; set; }

		[JsonProperty, Column(Name = "no")]
		public short? No { get; set; }

		[JsonProperty, Column(Name = "stated")]
		public short? Stated { get; set; }

		[JsonProperty, Column(Name = "type", StringLength = 20)]
		public string Type { get; set; }

		[JsonProperty, Column(Name = "unit", StringLength = 10)]
		public string Unit { get; set; }

		[JsonProperty, Column(Name = "viewed")]
		public short? Viewed { get; set; }

        [JsonProperty, Column(Name = "key_name")]
        public string KeyName { get; set; }

    }

}
