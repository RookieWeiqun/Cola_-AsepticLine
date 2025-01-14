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

	[JsonObject(MemberSerialization.OptIn), Table(Name = "device_info_detail", DisableSyncStructure = true)]
	public partial class DeviceInfoDetail {

		[JsonProperty, Column(Name = "dev_id", IsPrimary = true, IsIdentity = true, InsertValueSql = "nextval('device_info_detail_dev_id_seq'::regclass)")]
		public int DevId { get; set; }

		[JsonProperty, Column(Name = "para", StringLength = 50)]
		public string Para { get; set; }

		[JsonProperty, Column(Name = "value")]
		public double? Value { get; set; }

	}

}
