﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace GeneticAlgorithmTimeTable
{
    /// <summary>
    /// 알고리즘에 사용되는 상수들을 모아놓은 클래스
    /// </summary>
    class Constants
    {
        #region Singleton Pattern
        private static Constants instance;

        private Constants() { }

        public static Constants Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Constants();
                }
                return instance;
            }
        }
        #endregion

        /// <summary>
        /// 이론 2~3학점 수업이 시작 가능한 시간
        /// </summary>
        public List<double> AvailablePeriod_23Credit = new List<double> { 9.5, 11.0, 14.0, 15.5, 17.0 };

        /// <summary>
        /// 이론 3학점 수업이 선택 가능한 요일
        /// </summary>
        public List<CourseDay> AvailableDay_3Credit = new List<CourseDay> { CourseDay.AC, CourseDay.BD };

        /// <summary>
        /// 이론 2학점 수업이 선택 가능한 요일
        /// </summary>
        public List<CourseDay> AvailableDay_2Credit = new List<CourseDay> { CourseDay.AC, CourseDay.BD, CourseDay.A, CourseDay.B, CourseDay.C, CourseDay.D, CourseDay.E };

        /// <summary>
        /// 한 세대 인구 수
        /// </summary>
        public int PopulationSize { get; private set; }

        /// <summary>
        /// 엘리트 연산 선택 % 수
        /// </summary>
        public int ElitismPercentage { get; private set; }

        /// <summary>
        /// 교차 연산 %
        /// </summary>
        public double CrossoverProbability { get; private set; }

        /// <summary>
        /// 돌연변이 %
        /// </summary>
        public double MutateProbability { get; private set; }

        /// <summary>
        /// 새 랜덤 해 %
        /// </summary>
        public int RandomReplaceProbability { get; private set; }

        /// <summary>
        /// 탈출 조건
        /// </summary>
        public double TerminateFitness { get; private set; }

        /// <summary>
        /// 탈출 조건
        /// </summary>
        public int TerminateGeneration { get; private set; }
        
        public void Read()
        {
            StreamReader reader = new StreamReader("Input.txt");

            string line = reader.ReadLine();
            PopulationSize = Int32.Parse(line);
            line = reader.ReadLine();
            ElitismPercentage = Int32.Parse(line);
            line = reader.ReadLine();
            CrossoverProbability = Double.Parse(line);
            line = reader.ReadLine();
            RandomReplaceProbability = Int32.Parse(line);
            line = reader.ReadLine();
            MutateProbability = Double.Parse(line);
            line = reader.ReadLine();
            TerminateGeneration = Int32.Parse(line);
            line = reader.ReadLine();
            TerminateFitness = Double.Parse(line);

            reader.Close();
        }
    }
}
