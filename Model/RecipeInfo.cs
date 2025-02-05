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

	[JsonObject(MemberSerialization.OptIn), Table(Name = "recipe_info", DisableSyncStructure = true)]
	public partial class RecipeInfo {

		[JsonProperty, Column(Name = "id", IsPrimary = true, IsIdentity = true, InsertValueSql = "nextval('recipe_info_id_seq'::regclass)")]
		public int Id { get; set; }

		[JsonProperty, Column(Name = "alias_name", StringLength = 50)]
		public string AliasName { get; set; }

		[JsonProperty, Column(Name = "device_id")]
		public int? DeviceId { get; set; }

		[JsonProperty, Column(Name = "line_id")]
		public int? LineId { get; set; }

		[JsonProperty, Column(Name = "name", StringLength = 50)]
		public string Name { get; set; }

		[JsonProperty, Column(Name = "no")]
		public short? No { get; set; }

		[JsonProperty, Column(Name = "sku", StringLength = 10)]
		public string Sku { get; set; }

	}

}
