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

	[JsonObject(MemberSerialization.OptIn), Table(Name = "clean_state", DisableSyncStructure = true)]
	public partial class CleanState {

		[JsonProperty, Column(Name = "id", IsPrimary = true, IsIdentity = true, InsertValueSql = "nextval('clean_state_id_seq'::regclass)")]
		public int Id { get; set; }

		[JsonProperty, Column(Name = "description", StringLength = -2)]
		public string Description { get; set; }

		[JsonProperty, Column(Name = "name", StringLength = 20)]
		public string Name { get; set; }

		[JsonProperty, Column(Name = "value")]
		public short? Value { get; set; }

	}

}
