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

	[JsonObject(MemberSerialization.OptIn), Table(Name = "his_data_alarm", DisableSyncStructure = true)]
	public partial class HisDataAlarm {

		[JsonProperty, Column(Name = "id", IsPrimary = true, IsIdentity = true, InsertValueSql = "nextval('his_data_alarm_id_seq'::regclass)")]
		public long Id { get; set; }

		[JsonProperty, Column(Name = "data", DbType = "json")]
		public JToken Data { get; set; }

		[JsonProperty, Column(Name = "device_id")]
		public int? DeviceId { get; set; }

		[JsonProperty, Column(Name = "hour_check_status")]
		public short? HourCheckStatus { get; set; }

		[JsonProperty, Column(Name = "hour_check_text", StringLength = -2)]
		public string HourCheckText { get; set; }

		[JsonProperty, Column(Name = "hour_check_time", DbType = "timestamptz")]
		public DateTime? HourCheckTime { get; set; }

		[JsonProperty, Column(Name = "hour_check_user", StringLength = 50)]
		public string HourCheckUser { get; set; }

		[JsonProperty, Column(Name = "hour_check_valid")]
		public short? HourCheckValid { get; set; }

		[JsonProperty, Column(Name = "line_id")]
		public int? LineId { get; set; }

		[JsonProperty, Column(Name = "record_time", DbType = "timestamptz", InsertValueSql = "CURRENT_TIMESTAMP")]
		public DateTime? RecordTime { get; set; }

	}

}
