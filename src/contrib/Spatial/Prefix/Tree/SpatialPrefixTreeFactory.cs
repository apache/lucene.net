﻿/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;

namespace Lucene.Net.Spatial.Prefix.Tree
{
	public abstract class SpatialPrefixTreeFactory
	{
		private const double DEFAULT_GEO_MAX_DETAIL_KM = 0.001; //1m

		protected Dictionary<String, String> args;
		protected SpatialContext ctx;
		protected int? maxLevels;

		/**
		 * The factory  is looked up via "prefixTree" in args, expecting "geohash" or "quad".
		 * If its neither of these, then "geohash" is chosen for a geo context, otherwise "quad" is chosen.
		 */
		public static SpatialPrefixTree MakeSPT(Dictionary<String, String> args, ClassLoader classLoader, SpatialContext ctx)
		{
			SpatialPrefixTreeFactory instance;
			String cname;
			if (!args.TryGetValue("prefixTree", out cname) || cname == null)
				cname = ctx.IsGeo() ? "geohash" : "quad";
			if ("geohash".Equals(cname, StringComparison.InvariantCultureIgnoreCase))
				instance = new GeohashPrefixTree.Factory();
			else if ("quad".Equals(cname, StringComparison.InvariantCultureIgnoreCase))
				instance = new QuadPrefixTree.Factory();
			else
			{
				try
				{
					Class c = classLoader.loadClass(cname);
					instance = (SpatialPrefixTreeFactory)c.newInstance();
				}
				catch (Exception e)
				{
					throw new RuntimeException(e);
				}
			}
			instance.Init(args, ctx);
			return instance.newSPT();
		}

		protected void Init(Dictionary<String, String> args, SpatialContext ctx)
		{
			this.args = args;
			this.ctx = ctx;
			InitMaxLevels();
		}

		protected void InitMaxLevels()
		{
			String mlStr;
			if (args.TryGetValue("maxLevels", out mlStr) && mlStr != null)
			{
				maxLevels = int.Parse(mlStr);
				return;
			}

			double degrees;
			if (!args.TryGetValue("maxDetailDist", out mlStr) || mlStr == null)
			{
				if (!ctx.IsGeo())
				{
					return; //let default to max
				}
				degrees = DistanceUtils.Dist2Degrees(DEFAULT_GEO_MAX_DETAIL_KM, DistanceUnits.KILOMETERS.EarthRadius());
			}
			else
			{
				degrees = DistanceUtils.Dist2Degrees(double.Parse(mlStr), ctx.GetUnits().EarthRadius());
			}
			maxLevels = GetLevelForDistance(degrees) + 1; //returns 1 greater
		}

		/** Calls {@link SpatialPrefixTree#getLevelForDistance(double)}. */
		protected abstract int GetLevelForDistance(double degrees);

		protected abstract SpatialPrefixTree NewSPT();

	}
}
