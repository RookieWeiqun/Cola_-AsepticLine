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

	[JsonObject(MemberSerialization.OptIn), Table(Name = "recipe_detail_info", DisableSyncStructure = true)]
	public partial class RecipeDetailInfo {

		[JsonProperty, Column(Name = "id", IsPrimary = true, IsIdentity = true, InsertValueSql = "nextval('recipe_detail_info_id_seq'::regclass)")]
		public int Id { get; set; }

		[JsonProperty, Column(Name = "check_para_id")]
		public int? CheckParaId { get; set; }

		[JsonProperty, Column(Name = "lower", StringLength = 50)]
		public string Lower { get; set; }

		[JsonProperty, Column(Name = "lower_inc")]
		public short? LowerInc { get; set; }

		[JsonProperty, Column(Name = "recipe_id")]
		public int? RecipeId { get; set; }

		[JsonProperty, Column(Name = "upper", StringLength = 50)]
		public string Upper { get; set; }

		[JsonProperty, Column(Name = "upper_inc")]
		public short? UpperInc { get; set; }

	}

}
