﻿using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Globalization;
using System.Threading;

namespace Lucene.Net.Core.Support
{
    /// <summary>
    /// This test was added for .NET compatibility - LUCENENET specific
    /// 
    /// It tests the Lucene.Net.Util.ToStringUtils which was untested in the Java counterpart,
    /// but required some help to ensure .NET compatibility.
    /// </summary>
    public class TestToStringUtils : LuceneTestCase
    {
        CultureInfo originalCulture;
        public override void SetUp()
        {
            base.SetUp();
            originalCulture = Thread.CurrentThread.CurrentCulture;
        }

        public override void TearDown()
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
            base.TearDown();
        }

        /// <summary>
        /// Check to ensure that the Boost function is properly converted in every possible culture.
        /// </summary>
        [Test, LuceneNetSpecific]
        public void TestBoost()
        {
            float boostNormal = 1f;
            float boostFractional = 2.5f;
            float boostNonFractional = 5f;
            float boostLong = 1.111111111f;
            float boostZeroNonFractional = 0f;
            float boostZeroFractional = 0.123f;

            foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures | CultureTypes.NeutralCultures))
            {
                Thread.CurrentThread.CurrentCulture = culture;

                assertEquals("", ToStringUtils.Boost(boostNormal));
                assertEquals("^2.5", ToStringUtils.Boost(boostFractional));
                assertEquals("^5.0", ToStringUtils.Boost(boostNonFractional));
                assertEquals("^1.111111", ToStringUtils.Boost(boostLong));
                assertEquals("^0.0", ToStringUtils.Boost(boostZeroNonFractional));
                assertEquals("^0.123", ToStringUtils.Boost(boostZeroFractional));
            }
        }
    }
}
