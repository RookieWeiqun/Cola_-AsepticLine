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

		[JsonProperty, Column(Name = "alarm_text", StringLength = -2)]
		public string AlarmText { get; set; }

		[JsonProperty, Column(Name = "arrival_time", DbType = "timestamptz")]
		public DateTime? ArrivalTime { get; set; }

		[JsonProperty, Column(Name = "confirm_text", StringLength = -2)]
		public string ConfirmText { get; set; }

		[JsonProperty, Column(Name = "device_id")]
		public int? DeviceId { get; set; }

		[JsonProperty, Column(Name = "leave_time", DbType = "timestamptz")]
		public DateTime? LeaveTime { get; set; }

		[JsonProperty, Column(Name = "line_id")]
		public int? LineId { get; set; }

		[JsonProperty, Column(Name = "para_id")]
		public int? ParaId { get; set; }

		[JsonProperty, Column(Name = "recipe_id")]
		public int? RecipeId { get; set; }

		[JsonProperty, Column(Name = "record_time", DbType = "timestamptz", InsertValueSql = "CURRENT_TIMESTAMP")]
		public DateTime? RecordTime { get; set; }

	}

}
