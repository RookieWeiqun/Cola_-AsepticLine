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

	[JsonObject(MemberSerialization.OptIn), Table(Name = "device_info", DisableSyncStructure = true)]
	public partial class DeviceInfo {

		[JsonProperty, Column(Name = "id", IsPrimary = true, IsIdentity = true, InsertValueSql = "nextval('device_info_id_seq'::regclass)")]
		public int Id { get; set; }

		[JsonProperty, Column(Name = "alias_name", StringLength = 50)]
		public string AliasName { get; set; }

		[JsonProperty, Column(Name = "checked")]
		public short? Checked { get; set; }

		[JsonProperty, Column(Name = "device_type")]
		public int? DeviceType { get; set; }

		[JsonProperty, Column(Name = "line_id")]
		public int? LineId { get; set; }

		[JsonProperty, Column(Name = "name", StringLength = 50)]
		public string Name { get; set; }

		[JsonProperty, Column(Name = "no")]
		public short? No { get; set; }

		[JsonProperty, Column(Name = "pre_mixed")]
		public short? PreMixed { get; set; }

	}

}
