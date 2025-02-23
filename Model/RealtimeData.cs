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

	[JsonObject(MemberSerialization.OptIn), Table(Name = "realtime_data", DisableSyncStructure = true)]
	public partial class RealtimeData {

		[JsonProperty, Column(Name = "id", IsPrimary = true, IsIdentity = true, InsertValueSql = "nextval('realtime_data_id_seq'::regclass)")]
		public int Id { get; set; }

		[JsonProperty, Column(Name = "data", DbType = "json")]
		public JToken Data { get; set; }

		[JsonProperty, Column(Name = "device_id")]
		public int? DeviceId { get; set; }

		[JsonProperty, Column(Name = "line_id")]
		public int? LineId { get; set; }

		[JsonProperty, Column(Name = "recipe_id")]
		public int? RecipeId { get; set; }

		[JsonProperty, Column(Name = "update_time", DbType = "timestamptz", InsertValueSql = "CURRENT_TIMESTAMP")]
		public DateTime? UpdateTime { get; set; }

        [Navigate(nameof(DeviceId))]
        public DeviceInfo DeviceInfo { get; set; }= new DeviceInfo();

        [JsonProperty, Column(Name = "state_id")]
        public int? StateId { get; set; }

    }

}
