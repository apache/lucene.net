using System.Globalization;

namespace Lucene.Net.Search.Similarities
{
    /*
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

    /// <summary>
    /// Abstract superclass for language modeling Similarities. The following inner
    /// types are introduced:
    /// <list type="bullet">
    ///     <item><description><see cref="LMStats"/>, which defines a new statistic, the probability that
    ///         the collection language model generates the current term;</description></item>
    ///     <item><description><see cref="ICollectionModel"/>, which is a strategy interface for object that
    ///         compute the collection language model <c>p(w|C)</c>;</description></item>
    ///     <item><description><see cref="DefaultCollectionModel"/>, an implementation of the former, that
    ///         computes the term probability as the number of occurrences of the term in the
    ///         collection, divided by the total number of tokens.</description></item>
    /// </list>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class LMSimilarity : SimilarityBase
    {
        /// <summary>
        /// The collection model. </summary>
        protected readonly ICollectionModel m_collectionModel;

        /// <summary>
        /// Creates a new instance with the specified collection language model. </summary>
        public LMSimilarity(ICollectionModel collectionModel)
        {
            this.m_collectionModel = collectionModel;
        }

        /// <summary>
        /// Creates a new instance with the default collection language model. </summary>
        public LMSimilarity()
            : this(new DefaultCollectionModel())
        {
        }

        protected internal override BasicStats NewStats(string field, float queryBoost)
        {
            return new LMStats(field, queryBoost);
        }

        /// <summary>
        /// Computes the collection probability of the current term in addition to the
        /// usual statistics.
        /// </summary>
        protected internal override void FillBasicStats(BasicStats stats, CollectionStatistics collectionStats, TermStatistics termStats)
        {
            base.FillBasicStats(stats, collectionStats, termStats);
            LMStats lmStats = (LMStats)stats;
            lmStats.CollectionProbability = m_collectionModel.ComputeProbability(stats);
        }

        protected internal override void Explain(Explanation expl, BasicStats stats, int doc, float freq, float docLen)
        {
            expl.AddDetail(new Explanation(m_collectionModel.ComputeProbability(stats), "collection probability"));
        }

        /// <summary>
        /// Returns the name of the LM method. The values of the parameters should be
        /// included as well.
        /// <para>Used in <see cref="ToString()"/></para>.
        /// </summary>
        public abstract string GetName();

        /// <summary>
        /// Returns the name of the LM method. If a custom collection model strategy is
        /// used, its name is included as well. </summary>
        /// <seealso cref="GetName()"/>
        /// <seealso cref="ICollectionModel.GetName()"/>
        /// <seealso cref="DefaultCollectionModel"/>
        public override string ToString()
        {
            string coll = m_collectionModel.GetName();
            if (coll is object)
            {
                return string.Format("LM {0} - {1}", GetName(), coll);
            }
            else
            {
                return string.Format("LM {0}", GetName());
            }
        }

        /// <summary>
        /// Stores the collection distribution of the current term. </summary>
        public class LMStats : BasicStats
        {
            /// <summary>
            /// The probability that the current term is generated by the collection. </summary>
            private float collectionProbability;

            /// <summary>
            /// Creates <see cref="LMStats"/> for the provided field and query-time boost
            /// </summary>
            public LMStats(string field, float queryBoost)
                : base(field, queryBoost)
            {
            }

            /// <summary>
            /// Returns the probability that the current term is generated by the
            /// collection.
            /// </summary>
            public float CollectionProbability
            {
                get => collectionProbability;
                set => this.collectionProbability = value;
            }
        }

        /// <summary>
        /// A strategy for computing the collection language model. </summary>
        public interface ICollectionModel
        {
            /// <summary>
            /// Computes the probability <c>p(w|C)</c> according to the language model
            /// strategy for the current term.
            /// </summary>
            float ComputeProbability(BasicStats stats);

            /// <summary>
            /// The name of the collection model strategy. </summary>
            string GetName();
        }

        /// <summary>
        /// Models <c>p(w|C)</c> as the number of occurrences of the term in the
        /// collection, divided by the total number of tokens <c>+ 1</c>.
        /// </summary>
        public class DefaultCollectionModel : ICollectionModel
        {
            /// <summary>
            /// Sole constructor: parameter-free </summary>
            public DefaultCollectionModel()
            {
            }

            public virtual float ComputeProbability(BasicStats stats)
            {
                return (stats.TotalTermFreq + 1F) / (stats.NumberOfFieldTokens + 1F);
            }

            public virtual string GetName()
            {
                return null;
            }
        }
    }
}