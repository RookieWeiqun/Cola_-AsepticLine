﻿using FreeSql.DatabaseModel;using System;
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

	[JsonObject(MemberSerialization.OptIn), Table(Name = "line_info", DisableSyncStructure = true)]
	public partial class LineInfo {

		[JsonProperty, Column(Name = "id", IsPrimary = true, IsIdentity = true, InsertValueSql = "nextval('line_info_id_seq'::regclass)")]
		public int Id { get; set; }

		[JsonProperty, Column(Name = "alias_name", StringLength = 50, IsNullable = false)]
		public string AliasName { get; set; }

		[JsonProperty, Column(Name = "brand_id")]
		public short? BrandId { get; set; }

		[JsonProperty, Column(Name = "enabled")]
		public short? Enabled { get; set; }

		[JsonProperty, Column(Name = "name", StringLength = 50, IsNullable = false)]
		public string Name { get; set; }

		[JsonProperty, Column(Name = "no")]
		public short No { get; set; }

    }

}
