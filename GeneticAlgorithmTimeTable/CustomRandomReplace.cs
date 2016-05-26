
/*
	Genetic Algorithm Framework for .Net
	Copyright (C) 2016  John Newcombe

	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

		You should have received a copy of the GNU Lesser General Public License
		along with this program.  If not, see <http://www.gnu.org/licenses/>.

	http://johnnewcombe.net
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using GAF.Extensions;
using GAF.Threading;
using GAF;

namespace GeneticAlgorithmTimeTable
{
    /// <summary>
    /// This operator will replace the weakest solutions in the new population 
    /// with the selected amount (by percentatge) of randomly generated solutions 
    /// (Chromosomes) from the current population. Any chromosome marked as Elite
    /// will not be replaced. Therefore, 50% of a population of 100 that has 10
    /// 'Elites' will replace 45 solutions.
    /// </summary>
    public class CustomRandomReplace : IGeneticOperator
    {
        private readonly object _syncLock = new object();
        private FitnessFunction _fitnessFunctionDelegate;
        private int _evaluations;

        private int _percentageToReplace;

        private List<Course> _courses;

        /// <summary>
        /// Replaces the whole population with randomly generated solutions.
        /// </summary>
        internal CustomRandomReplace()
        {
        }
        
        /// <summary>
        /// Replaces the specified number of the weakest individuals, with randomly generated ones.
        /// </summary>
        /// <param name="percentageToReplace">Set the number to replace.</param>
        public CustomRandomReplace(int percentageToReplace, List<Course> courses)
        {
            _percentageToReplace = percentageToReplace;
            _courses = courses;
            Enabled = true;

        }

        /// <summary>
        /// Enabled property. Diabling this operator will cause the population to 'pass through' unaltered.
        /// </summary>
        public bool Enabled { set; get; }

        /// <summary>
        /// This is the method that invokes the operator. This should not normally be called explicitly.
        /// </summary>
        /// <param name="currentPopulation"></param>
        /// <param name="newPopulation"></param>
        /// <param name="fitnessFunctionDelegate"></param>
        public void Invoke(Population currentPopulation, ref Population newPopulation, FitnessFunction fitnessFunctionDelegate)
        {
            //if the new population is null, create an empty population
            if (newPopulation == null)
            {
                newPopulation = currentPopulation.CreateEmptyCopy();
            }

            if (!Enabled) return;

            if (currentPopulation.Solutions == null || currentPopulation.Solutions.Count == 0)
            {
                throw new ArgumentException("There are no Solutions in the current Population.");
            }

            _fitnessFunctionDelegate = fitnessFunctionDelegate;

            Replace(currentPopulation, ref newPopulation, this.Percentage, _fitnessFunctionDelegate);

        }

        /// <summary>
        /// Helper Method marked as Internal for Unit Testing purposes.
        /// </summary>
        /// <param name="currentPopulation"></param>
        /// <param name="newPopulation"></param>
        /// <param name="percentage"></param>
        /// <param name="fitnessFunctionDelegate"></param>
        internal void Replace(Population currentPopulation, ref Population newPopulation, int percentage, FitnessFunction fitnessFunctionDelegate)
        {

            //copy everything accross in order of fitness i.e. Elites at the top
            newPopulation.Solutions.AddRange(currentPopulation.Solutions);
            newPopulation.Solutions.Sort();

            //find the number of non elites
            var chromosomeCount = newPopulation.Solutions.Count(s => !s.IsElite);

            //determine how many we are replacing based on the percentage
            var numberToReplace = (int)System.Math.Round((chromosomeCount / 100.0) * percentage);

            //we fill it up if we are short.
            if (numberToReplace > chromosomeCount)
            {
                numberToReplace = chromosomeCount;
            }

            if (numberToReplace > 0)
            {
                //we are adding random imigrants to the new population
                if (newPopulation == null || newPopulation.PopulationSize < numberToReplace)
                {
                    throw new ArgumentException(
                        "The 'newPopulation' does not contain enough solutions for the current operation.");
                }

                //reduce the population as required
                newPopulation.Solutions.RemoveRange(chromosomeCount - numberToReplace, numberToReplace);

                var chromosomeLength = currentPopulation.ChromosomeLength;

                //var immigrants = new List<Chromosome>();
                for (var index = 0; index < numberToReplace; index++)
                {
                    // chromosome은 하나의 해 즉, TimeTable을 의미한다
                    var chromosome = new Chromosome();

                    // chromosome에 모든 코스에 해당하는 gene이 정해진 순서대로 배치되도록 한다.
                    foreach (var course in _courses)
                    {
                        // Gene은 시간이 할당된 수업을 의미한다
                        // 새로운 CourseGene 객체를 생성하면 시간이 랜덤으로 배정된다.
                        CourseGene courseGene = new CourseGene(course);

                        // Gene List에 추가
                        chromosome.Genes.Add(new Gene(courseGene));
                    }

                    AddImigrant(newPopulation, chromosome, fitnessFunctionDelegate);
                }

            }


        }

        private void AddImigrant(Population population, Chromosome imigrant, FitnessFunction fitnessFunctionDelegate)
        {
            //need to add these to the solution, sort and then remove the weakest
            if (imigrant != null && population != null)
            {

                imigrant.Evaluate(fitnessFunctionDelegate);
                _evaluations++;

                //TODO: Fix this, Random does not want to remove weakest as we are trying to increase diversity.

                //add the imigrant this extends the population
                population.Solutions.Add(imigrant);

            }
        }
        
        /// <summary>
        /// Returns the number of evaluations performed by this operator.
        /// </summary>
        /// <returns></returns>
        public int GetOperatorInvokedEvaluations()
        {
            return _evaluations;
        }

        /// <summary>
        /// Sets/Gets the Percentage number to be replaced. The setting and getting of this property is thread safe.
        /// </summary>
        public int Percentage
        {
            get
            {
                //not really needed as 32bit int updates are atomic on 32bit systems 
                lock (_syncLock)
                {
                    return _percentageToReplace;
                }
            }

            set
            {
                lock (_syncLock)
                {
                    _percentageToReplace = value;
                }
            }
        }
    }
}