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

	[JsonObject(MemberSerialization.OptIn), Table(Name = "sys_config", DisableSyncStructure = true)]
	public partial class SysConfig {

		[JsonProperty, Column(Name = "id", IsPrimary = true, IsIdentity = true, InsertValueSql = "nextval('sys_config_id_seq'::regclass)")]
		public int Id { get; set; }

		[JsonProperty, Column(Name = "description", StringLength = -2)]
		public string Description { get; set; }

		[JsonProperty, Column(Name = "key", StringLength = 50)]
		public string Key { get; set; }

		[JsonProperty, Column(Name = "key_name", StringLength = 50)]
		public string KeyName { get; set; }

		[JsonProperty, Column(Name = "value", StringLength = 50)]
		public string Value { get; set; }

	}

}
